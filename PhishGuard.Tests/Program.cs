using System.Text;
using System.Text.Json;
using System.IO;

namespace PhishGuard.Tests;

public class Program
{
    public static async Task Main(string[] args)
    {
        var httpClient = new HttpClient();
        var apiUrl = "http://localhost:5204/predict";

        var testFile = "phishing_dataset.csv";
        if (!File.Exists(testFile))
        {
            Console.WriteLine($"❌ {testFile} not found. Place it in the same folder as this EXE.");
            return;
        }

        var results = new List<TestResult>();
        var lines = File.ReadAllLines(testFile).Skip(1); // Skip header

        Console.WriteLine("🧪 Running PhishGuard evaluation on synthetic URLs...\n");

        // Track sections for clearer output
        bool homographSection = false;
        bool legitimateSection = false;

        foreach (var line in lines)
        {
            // Skip comment lines
            if (line.TrimStart().StartsWith("#")) continue;

            var parts = ParseCsvLine(line);
            if (parts.Length < 2) continue;

            var url = parts[0];
            var expected = parts[1].Equals("true", StringComparison.OrdinalIgnoreCase);

            // Detect section headers for console output
            if (url.Contains("HOMOGRAPH-ONLY") && !homographSection)
            {
                Console.WriteLine("\n🔍 HOMOGRAPH-ONLY VALIDATION");
                homographSection = true;
                continue;
            }
            if (url.Contains("LEGITIMATE SITE") && !legitimateSection)
            {
                Console.WriteLine("\n🔍 LEGITIMATE SITE EXPANSION VALIDATION");
                legitimateSection = true;
                continue;
            }

            try
            {
                var json = JsonSerializer.Serialize(new { url });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(apiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    results.Add(new TestResult(url, expected, "error", false, 0f, $"HTTP {response.StatusCode}"));
                    continue;
                }

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var root = doc.RootElement;
                var isPhishing = root.GetProperty("isPhishing").GetBoolean();
                var confidence = root.TryGetProperty("confidence", out var conf) ? conf.GetSingle() : 0f;
                var explanation = root.TryGetProperty("explanation", out var expl) ? expl.GetString() ?? "" : "";

                results.Add(new TestResult(url, expected, "test", isPhishing, confidence, explanation));

                // Enhanced console output: show explanation for phishing detections
                if (isPhishing)
                {
                    Console.WriteLine($"✅ {url} → PHISHING (Expected: {(expected ? "PHISHING" : "SAFE")})");
                    Console.WriteLine($"   Explanation: \"{explanation}\"");
                }
                else
                {
                    Console.WriteLine($"✅ {url} → SAFE (Expected: {(expected ? "PHISHING" : "SAFE")})");
                }
            }
            catch (Exception ex)
            {
                results.Add(new TestResult(url, expected, "error", false, 0f, ex.Message));
                Console.WriteLine($"❌ {url} → ERROR: {ex.Message}");
            }

            await Task.Delay(5); // Reduced delay for faster execution
        }

        // Save detailed results
        var outputFile = "test_results.csv";
        await File.WriteAllTextAsync(outputFile, BuildCsv(results));
        Console.WriteLine($"\n✅ All results saved to: {Path.GetFullPath(outputFile)}");

        // Calculate comprehensive metrics
        var total = results.Count(r => r.Category != "error");
        var truePositives = results.Count(r => r.Expected && r.Predicted == true);
        var falsePositives = results.Count(r => !r.Expected && r.Predicted == true);
        var trueNegatives = results.Count(r => !r.Expected && r.Predicted == false);
        var falseNegatives = results.Count(r => r.Expected && r.Predicted == false);

        var accuracy = total > 0 ? (double)(truePositives + trueNegatives) / total * 100 : 0;
        var precision = (truePositives + falsePositives) > 0 ? (double)truePositives / (truePositives + falsePositives) * 100 : 0;
        var recall = (truePositives + falseNegatives) > 0 ? (double)truePositives / (truePositives + falseNegatives) * 100 : 0;
        var f1 = (precision + recall) > 0 ? 2 * (precision * recall) / (precision + recall) : 0;
        var fpr = (falsePositives + trueNegatives) > 0 ? (double)falsePositives / (falsePositives + trueNegatives) * 100 : 0;

        Console.WriteLine("\n📊 EVALUATION METRICS:");
        Console.WriteLine($"   Total Valid Tests: {total}");
        Console.WriteLine($"   Accuracy:    {accuracy:F2}%");
        Console.WriteLine($"   Precision:   {precision:F2}%");
        Console.WriteLine($"   Recall:      {recall:F2}%");
        Console.WriteLine($"   F1-Score:    {f1:F2}%");
        Console.WriteLine($"   False Positive Rate: {fpr:F2}%");
    }

    // Robust CSV parser that handles quoted fields
    static string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = "";
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"' && (i == 0 || line[i - 1] != '\\'))
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }
        values.Add(current);
        return values.ToArray();
    }

    static string BuildCsv(List<TestResult> results)
    {
        var csv = new StringBuilder();
        csv.AppendLine("url,expected,predicted,correct,confidence,explanation,category");
        foreach (var r in results)
        {
            var correct = r.Predicted.HasValue && r.Predicted.Value == r.Expected;
            // Escape quotes in explanation
            var safeExplanation = r.Explanation.Replace("\"", "\"\"");
            csv.AppendLine($"\"{r.Url}\",{r.Expected},{r.Predicted},{correct},{r.Confidence},\"{safeExplanation}\",{r.Category}");
        }
        return csv.ToString();
    }
}

record TestResult
{
    public string Url { get; init; }
    public bool Expected { get; init; }
    public string Category { get; init; }
    public bool? Predicted { get; init; }
    public float Confidence { get; init; }
    public string Explanation { get; init; }

    public TestResult(string url, bool expected, string category, bool predicted, float confidence, string explanation)
    {
        Url = url;
        Expected = expected;
        Category = category;
        Predicted = predicted;
        Confidence = confidence;
        Explanation = explanation;
    }
}