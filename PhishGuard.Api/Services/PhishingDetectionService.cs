using Microsoft.ML;
using Microsoft.ML.Data;
using System.IO;
using System;
using System.Linq;

namespace PhishGuard.Api.Services;

public class PhishingDetectionService
{
    private readonly PredictionEngine<UrlPredictionInput, UrlPrediction> _predictionEngine;

    // High-risk brands commonly impersonated
    private static readonly string[] BrandKeywords = {
        "instagram", "facebook", "paypal", "microsoft", "apple",
        "amazon", "netflix", "google", "linkedin", "twitter", "roblox", "steam", "wellsfargo"
    };

    // Suspicious TLDs and patterns (expanded based on your dataset)
    private static readonly string[] SuspiciousTlds = {
        ".xyz", ".top", ".club", ".online", ".site", ".click", ".link",
        ".space", ".shop", ".work", ".gq", ".ml", ".cf", ".zulu",
        ".cc", ".bond", ".eu.org", ".cfd", ".life", ".news"
    };

    public PhishingDetectionService()
    {
        var mlContext = new MLContext();
        var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MLModel.zip");

        if (!File.Exists(modelPath))
            throw new FileNotFoundException("MLModel.zip not found.");

        ITransformer mlModel = mlContext.Model.Load(modelPath, out var _);
        _predictionEngine = mlContext.Model.CreatePredictionEngine<UrlPredictionInput, UrlPrediction>(mlModel);
    }

    /// <summary>
    /// Main prediction method using hybrid ML + rules
    /// </summary>
    public async Task<PredictionResult> PredictAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) ||
            !(uriResult.Scheme == "http" || uriResult.Scheme == "https"))
        {
            return new PredictionResult { IsPhishing = false, Confidence = 0f, Explanation = "Skipped: Non-web URL." };
        }

        try
        {
            bool isRuleBasedPhishing = IsRuleBasedPhishing(url);
            var input = new UrlPredictionInput { Url = url };
            var prediction = _predictionEngine.Predict(input);
            string explanation = GenerateExplanation(url, uriResult, prediction);

            bool finalDecision = isRuleBasedPhishing || prediction.Prediction;
            float confidence = isRuleBasedPhishing ? 0.95f : prediction.Probability;

            return new PredictionResult
            {
                IsPhishing = finalDecision,
                Confidence = confidence,
                Explanation = explanation
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ML Error] {ex.Message}");
            return GetRuleBasedPrediction(url);
        }
    }

    /// <summary>
    /// Pure rule-based baseline
    /// </summary>
    public PredictionResult GetRuleBasedPrediction(string url)
    {
        bool isPhishing = IsRuleBasedPhishing(url);
        string explanation = isPhishing
            ? GenerateRuleBasedExplanation(url)
            : "No threat detected.";

        return new PredictionResult
        {
            IsPhishing = isPhishing,
            Confidence = isPhishing ? 0.99f : 0.01f,
            Explanation = explanation
        };
    }

    /// <summary>
    /// Core rule logic: detects phishing heuristics (including on trusted platforms)
    /// </summary>
    private bool IsRuleBasedPhishing(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            return false;

        string host = uri.Host.ToLower();

        // 🔥 Detect brand impersonation anywhere in URL
        bool hasBrandImpersonation = BrandKeywords.Any(brand =>
            url.ToLower().Contains(brand) || IsTyposquatting(host, brand));

        // 🔥 FLAG SUSPICIOUS TLDs ALONE (no keyword required)
        bool hasSuspiciousTld = SuspiciousTlds.Any(tld => host.EndsWith(tld));

        // Risky keywords (for additional context)
        bool hasRiskyKeyword = url.Contains("login") || url.Contains("secure") ||
                              url.Contains("verify") || url.Contains("account") ||
                              url.Contains("signin") || url.Contains("update");

        // Unicode homograph detection
        bool hasNonAscii = url.Any(c => c > 127);

        // Known spoofed brands
        var spoofedBrands = new[] { "micosoft", "facebok", "gogle", "amaz0n", "paypa1", "appl3", "netfliix", "wallcfargo" };
        bool hasSpoofedBrand = spoofedBrands.Any(brand => url.Contains(brand, StringComparison.OrdinalIgnoreCase));

        // 🔥 KEY CHANGE: Flag TLDs independently
        return hasBrandImpersonation ||
               hasSuspiciousTld ||  // ← This is the critical fix
               hasNonAscii ||
               hasSpoofedBrand;
    }

    /// <summary>
    /// Detects common typosquatting patterns for major brands
    /// </summary>
    private bool IsTyposquatting(string host, string brand)
    {
        var typos = new Dictionary<string, string[]>
        {
            ["wellsfargo"] = ["wallcfargo", "wellsfarg0", "wellfargo", "wfargo"],
            ["instagram"] = ["insta", "instgram", "instagrm", "instagran"],
            ["facebook"] = ["facebok", "faceboook", "fb", "facebbook"],
            ["amazon"] = ["amaz0n", "amazom", "amzon"],
            ["apple"] = ["appl3", "aple", "appel"],
            ["paypal"] = ["paypa1", "paypai", "paypol"],
            ["roblox"] = ["robiox", "roblx", "robloxx"]
        };

        if (typos.TryGetValue(brand, out var variants))
        {
            return variants.Any(variant => host.Contains(variant));
        }
        return false;
    }

    /// <summary>
    /// Generates explanation for rule-based alerts
    /// </summary>
    private string GenerateRuleBasedExplanation(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            return "Invalid URL format.";

        var reasons = new System.Collections.Generic.List<string>();
        string host = uri.Host.ToLower();

        // Brand impersonation
        foreach (var brand in BrandKeywords)
        {
            if (url.ToLower().Contains(brand) || IsTyposquatting(host, brand))
            {
                reasons.Add($"Brand impersonation: '{brand}' detected.");
                break;
            }
        }

        // Unicode homograph
        if (url.Any(c => c > 127))
        {
            reasons.Add("Non-ASCII characters detected (homograph risk).");
        }

        // Suspicious TLDs
        if (SuspiciousTlds.Any(tld => host.EndsWith(tld)))
        {
            reasons.Add("High-risk top-level domain.");
        }

        // Risky keywords
        if (url.Contains("login") || url.Contains("secure") || url.Contains("verify"))
        {
            reasons.Add("High-risk keyword present.");
        }

        // Spoofed brands
        var spoofs = new[] { "micosoft", "facebok", "gogle", "amaz0n", "paypa1", "appl3", "netfliix", "wallcfargo" };
        foreach (var s in spoofs)
        {
            if (url.Contains(s, StringComparison.OrdinalIgnoreCase))
            {
                reasons.Add($"Detected known spoofed brand: '{s}'.");
                break;
            }
        }

        return reasons.Any()
            ? string.Join(" ", reasons.Take(2))
            : "Rule-based alert: suspicious pattern detected.";
    }

    /// <summary>
    /// Explanation for hybrid ML model
    /// </summary>
    private string GenerateExplanation(string url, Uri uri, UrlPrediction pred)
    {
        var reasons = new System.Collections.Generic.List<string>();
        string host = uri.Host.ToLower();

        // Brand impersonation
        foreach (var brand in BrandKeywords)
        {
            if (url.ToLower().Contains(brand) || IsTyposquatting(host, brand))
            {
                reasons.Add($"Brand impersonation: '{brand}' detected.");
                break;
            }
        }

        // Unicode homograph
        if (url.Any(c => c > 127))
        {
            reasons.Add("Non-ASCII characters detected (homograph risk).");
        }

        // Suspicious TLDs
        if (SuspiciousTlds.Any(tld => host.EndsWith(tld)))
        {
            reasons.Add("High-risk top-level domain.");
        }

        // Keywords
        if (url.Contains("login") || url.Contains("secure") || url.Contains("verify"))
        {
            reasons.Add("High-risk keyword present.");
        }

        if (!reasons.Any())
        {
            reasons.Add(pred.Prediction ? "ML model detected phishing patterns." : "No suspicious indicators.");
        }

        return string.Join(" ", reasons.Take(2));
    }
}

public class UrlPredictionInput
{
    public string Url { get; set; } = "";
}

public class UrlPrediction
{
    [ColumnName("PredictedLabel")]
    public bool Prediction { get; set; }

    [ColumnName("Probability")]
    public float Probability { get; set; }
}

public class PredictionResult
{
    public bool IsPhishing { get; set; }
    public float Confidence { get; set; }
    public string Explanation { get; set; } = "";
}