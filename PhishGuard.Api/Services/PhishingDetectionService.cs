using Microsoft.ML;
using Microsoft.ML.Data;
using System.IO;
using System;
using System.Linq;

namespace PhishGuard.Api.Services;

public class PhishingDetectionService
{
    private readonly PredictionEngine<UrlPredictionInput, UrlPrediction> _predictionEngine;

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
        // 🔑 URL VALIDATION: Only process valid HTTP/HTTPS URLs
        if (string.IsNullOrWhiteSpace(url) ||
            !Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) ||
            !(uriResult.Scheme == "http" || uriResult.Scheme == "https"))
        {
            return new PredictionResult
            {
                IsPhishing = false,
                Confidence = 0f,
                Explanation = "Skipped: Non-web URL scheme."
            };
        }

        try
        {
            var input = new UrlPredictionInput { Url = url };
            var prediction = _predictionEngine.Predict(input);
            string explanation = GenerateExplanation(url, prediction);

            return new PredictionResult
            {
                IsPhishing = prediction.Prediction,
                Confidence = prediction.Probability,
                Explanation = explanation
            };
        }
        catch
        {
            // Fallback to robust rule-based detection
            return GetRuleBasedPrediction(url);
        }
    }

    /// <summary>
    /// Pure rule-based baseline for experimental comparison
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
    /// Core rule logic: returns true if URL matches phishing heuristics
    /// </summary>
    private bool IsRuleBasedPhishing(string url)
    {
        // 1. Known spoofed brands (homograph attacks)
        var spoofedBrands = new[] { "micosoft", "facebok", "gogle", "amaz0n", "paypa1", "appl3", "netfliix", "ebayy", "micros0ft", "faceb00k" };
        bool hasSpoofedBrand = spoofedBrands.Any(brand => url.Contains(brand, StringComparison.OrdinalIgnoreCase));

        // 2. Rare TLD + high-risk keyword combo
        var rareTlds = new[] { ".xyz", ".gq", ".top", ".ml", ".cf", ".shop", ".click", ".link", ".zulu" };
        var riskyKeywords = new[] { "login", "secure", "verify", "account", "signin", "update", "confirm", "banking" };

        bool hasRareTld = rareTlds.Any(tld => url.Contains(tld, StringComparison.OrdinalIgnoreCase));
        bool hasRiskyKeyword = riskyKeywords.Any(kw => url.Contains(kw, StringComparison.OrdinalIgnoreCase));

        // Trigger if: (spoofed brand) OR (rare TLD + risky keyword)
        return hasSpoofedBrand || (hasRareTld && hasRiskyKeyword);
    }

    /// <summary>
    /// Generates explanation for rule-based alerts
    /// </summary>
    private string GenerateRuleBasedExplanation(string url)
    {
        var reasons = new System.Collections.Generic.List<string>();

        // Brand spoofs
        var spoofs = new[] { "micosoft", "facebok", "gogle", "amaz0n", "paypa1", "appl3", "netfliix" };
        foreach (var s in spoofs)
        {
            if (url.Contains(s, StringComparison.OrdinalIgnoreCase))
            {
                reasons.Add($"Detected known spoofed brand: '{s}'.");
                break;
            }
        }

        // Rare TLDs
        if (url.Contains(".xyz") || url.Contains(".top") || url.Contains(".gq") || url.Contains(".ml") || url.Contains(".cf"))
        {
            reasons.Add("Uses uncommon or risky top-level domain.");
        }

        // Keywords
        if (url.Contains("login") || url.Contains("secure") || url.Contains("verify"))
        {
            reasons.Add("Contains high-risk keyword (e.g., 'login').");
        }

        return reasons.Any()
            ? string.Join(" ", reasons.Take(2))
            : "Rule-based alert: known spoof pattern.";
    }

    /// <summary>
    /// Explanation for hybrid ML model (used in main prediction)
    /// </summary>
    private string GenerateExplanation(string url, UrlPrediction pred)
    {
        var reasons = new System.Collections.Generic.List<string>();

        // Brand spoofs
        var spoofs = new[] { "micosoft", "facebok", "gogle", "appl3", "paypa1", "netfliix" };
        foreach (var s in spoofs)
        {
            if (url.Contains(s, StringComparison.OrdinalIgnoreCase))
            {
                reasons.Add($"Detected known spoofed brand: '{s}'.");
                break;
            }
        }

        // Rare TLDs
        if (url.Contains(".xyz") || url.Contains(".top") || url.Contains(".gq") || url.Contains(".ml") || url.Contains(".cf"))
        {
            reasons.Add("Uses uncommon or risky top-level domain.");
        }

        // Keywords
        if (url.Contains("login") || url.Contains("secure") || url.Contains("verify"))
        {
            reasons.Add("Contains high-risk keyword (e.g., 'login').");
        }

        if (!reasons.Any())
        {
            reasons.Add(pred.Prediction ? "Model detected phishing patterns." : "No suspicious indicators.");
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