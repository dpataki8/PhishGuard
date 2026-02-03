using System.Text;
using System.Text.Json;
using System.IO;

namespace PhishGuard.Tests;

public class Program
{
    public static async Task Main(string[] args)
    {
        var httpClient = new HttpClient();
        var apiUrl = "http://localhost:5204/predict"; // 🔑 Updated to HTTP port 5204

        var testFile = "phishing_dataset.csv"; // 🔑 Matches your dataset name
        if (!File.Exists(testFile))
        {
            Console.WriteLine($"❌ {testFile} not found. Place it in the same folder as this EXE.");
            return;
        }

        var results = new List<TestResult>();
        var lines = File.ReadAllLines(testFile).Skip(1); // Skip header

        Console.WriteLine("🧪 Running PhishGuard evaluation on 500 synthetic URLs...\n");

        foreach (var line in lines)
        {
            var parts = line.Split(',');
            if (parts.Length < 2) continue;

            var url = parts[0];
            var expected = parts[1].Equals("true", StringComparison.OrdinalIgnoreCase);

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
                Console.WriteLine($"✅ {url} → {(isPhishing ? "PHISHING" : "SAFE")} (Expected: {(expected ? "PHISHING" : "SAFE")})");
            }
            catch (Exception ex)
            {
                results.Add(new TestResult(url, expected, "error", false, 0f, ex.Message));
                Console.WriteLine($"❌ {url} → ERROR: {ex.Message}");
            }

            await Task.Delay(10); // Small delay to avoid overwhelming API
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

        // Print full evaluation metrics
        Console.WriteLine("\n📊 EVALUATION METRICS:");
        Console.WriteLine($"   Total Valid Tests: {total}");
        Console.WriteLine($"   Accuracy:    {accuracy:F2}%");
        Console.WriteLine($"   Precision:   {precision:F2}%");
        Console.WriteLine($"   Recall:      {recall:F2}%");
        Console.WriteLine($"   F1-Score:    {f1:F2}%");
        Console.WriteLine($"   False Positive Rate: {fpr:F2}%");
    }

    static string BuildCsv(List<TestResult> results)
    {
        var csv = new StringBuilder();
        csv.AppendLine("url,expected,predicted,correct,confidence,explanation,category");
        foreach (var r in results)
        {
            var correct = r.Predicted.HasValue && r.Predicted.Value == r.Expected;
            csv.AppendLine($"\"{r.Url}\",{r.Expected},{r.Predicted},{correct},{r.Confidence},\"{r.Explanation}\",{r.Category}");
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
