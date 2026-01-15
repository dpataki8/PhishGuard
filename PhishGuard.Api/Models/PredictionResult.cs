namespace PhishGuard.Api.Models;

public class PredictionResult
{
    public bool IsPhishing { get; set; }
    public float Confidence { get; set; }
    public string Explanation { get; set; } = "No analysis available.";
}