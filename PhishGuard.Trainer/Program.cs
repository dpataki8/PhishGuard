using Microsoft.ML;
using Microsoft.ML.Data;
using System.Text;

try
{
    var mlContext = new MLContext(seed: 1);
    var random = new Random(42);
    var records = new List<UrlData>();

    // 🔹 LEGITIMATE URLS (200 samples)
    var legitDomains = new[] { "microsoft.com", "github.com", "google.com", "amazon.co.uk", "stackoverflow.com", "linkedin.com", "twitter.com", "instagram.com", "ebay.co.uk", "paypal.com" };
    var legitPaths = new[] { "/login", "/account", "/profile", "/", "/docs", "/signin", "/settings" };
    for (int i = 0; i < 250; i++)
    {
        var domain = legitDomains[random.Next(legitDomains.Length)];
        var path = legitPaths[random.Next(legitPaths.Length)];
        records.Add(new UrlData { Url = $"https://{domain}{path}", IsPhishing = false });
    }

    // 🔹 PHISHING: Keyword + Rare TLD (150 samples)
    var phishingTlds = new[] { ".xyz", ".top", ".gq", ".ml", ".cf", ".shop", ".click", ".link", ".zulu" }; // ← Emerging TLDs added here
    var phishingKeywords = new[] { "login", "secure", "verify", "account", "update", "signin", "confirm" };
    var brandSpoofs = new[] { "micosoft", "gogle", "amaz0n", "facebok", "appl3", "netfliix", "paypa1", "ebayy" };
    for (int i = 0; i < 200; i++)
    {
        var tld = phishingTlds[random.Next(phishingTlds.Length)];
        var brand = brandSpoofs[random.Next(brandSpoofs.Length)];
        var keyword = phishingKeywords[random.Next(phishingKeywords.Length)];
        records.Add(new UrlData { Url = $"http://{brand}-{keyword}{tld}/", IsPhishing = true });
    }

    // 🔹 NEW: HOMOGRAPH-ONLY PHISHING (NO KEYWORDS!) — 50 samples
    var homographDomains = new[] {
    "facebok.com", "gogle.com", "micosoft.net", "appl3.org", "paypa1.co",
    "netfliix.uk", "amaz0n.de", "micros0ft.fr", "ebay-security.ca", "appleid-support.io",
    "faceb00k.com", "g00gle.net", "m1crosoft.org", "p4ypal.co.uk", "l1nked1n.site"
};
    for (int i = 0; i < 50; i++)
    {
        var domain = homographDomains[random.Next(homographDomains.Length)];
        records.Add(new UrlData { Url = $"http://{domain}/", IsPhishing = true });
    }
    // Save dataset
    var csvPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "phishing_dataset.csv");
    csvPath = Path.GetFullPath(csvPath);
    Directory.CreateDirectory(Path.GetDirectoryName(csvPath)!);
    using (var writer = new StreamWriter(csvPath))
    {
        writer.WriteLine("Url,IsPhishing");
        foreach (var r in records)
            writer.WriteLine($"{r.Url},{r.IsPhishing}");
    }
    Console.WriteLine($"✅ Dataset saved to: {csvPath} ({records.Count} samples)");

    // Train model
    var dataView = mlContext.Data.LoadFromEnumerable(records);
    var pipeline = mlContext.Transforms.Text.FeaturizeText("UrlFeatures", "Url")
        .Append(mlContext.Transforms.Concatenate("Features", "UrlFeatures"))
        .Append(mlContext.BinaryClassification.Trainers.FastTree(
            labelColumnName: nameof(UrlData.IsPhishing),
            featureColumnName: "Features"));

    Console.WriteLine("⏳ Training model...");
    var model = pipeline.Fit(dataView);

    // Save model
    var modelPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "MLModel.zip");
    modelPath = Path.GetFullPath(modelPath);
    mlContext.Model.Save(model, dataView.Schema, modelPath);
    Console.WriteLine($"✅ Model saved to: {modelPath}");

    // Evaluate
    var predictions = model.Transform(dataView);
    var metrics = mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: nameof(UrlData.IsPhishing));
    Console.WriteLine($"📊 Accuracy: {metrics.Accuracy:P2}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ FATAL ERROR: {ex}");
    if (Environment.UserInteractive)
    {
        Console.WriteLine("\nPress Enter to exit...");
        Console.ReadLine();
    }
}

// Data classes
public class UrlData
{
    [LoadColumn(0)] public string Url { get; set; } = string.Empty;
    [LoadColumn(1)] public bool IsPhishing { get; set; }
}

public class UrlPrediction
{
    [ColumnName("PredictedLabel")] public bool Prediction { get; set; }
    [ColumnName("Probability")] public float Probability { get; set; }
}