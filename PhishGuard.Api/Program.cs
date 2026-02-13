using Microsoft.AspNetCore.StaticFiles;
using PhishGuard.Api.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// 🔑 Explicitly bind to all interfaces on port 5204
builder.WebHost.UseUrls("http://0.0.0.0:5204");

// 🔑 Enable CORS for Chrome extensions and localhost
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // Allow localhost origins
        policy.WithOrigins("http://localhost:5204", "https://localhost:7204")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("*");

        // 🔑 CRITICAL: Allow Chrome extension origins (e.g., chrome-extension://...)
        policy.SetIsOriginAllowed(origin => origin.StartsWith("chrome-extension://"));
    });
});

// Register your phishing detection service
builder.Services.AddScoped<PhishingDetectionService>();

var app = builder.Build();

// 🔑 Apply CORS policy BEFORE other middleware
app.UseCors("AllowAll");

// Serve static files (e.g., micosoft-login.xyz.html)
app.UseStaticFiles();

// API endpoint for phishing prediction
app.MapPost("/predict", async (HttpContext context, PhishingDetectionService service) =>
{
    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        using var doc = JsonDocument.Parse(body);
        var url = doc.RootElement.GetProperty("url").GetString() ?? "";

        var result = await service.PredictAsync(url);
        return Results.Json(result);
    }
    catch (Exception ex)
    {
        // Log error if needed: Console.WriteLine($"Prediction error: {ex}");
        return Results.Json(
            new { isPhishing = false, confidence = 0f, explanation = "Analysis failed." },
            statusCode: 500
        );
    }
});

// Optional: Add a health check
app.MapGet("/", () => "PhishGuard API is running on http://0.0.0.0:5204");

app.Run();