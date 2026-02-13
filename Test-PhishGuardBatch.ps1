# Test-PhishGuardBatch.ps1
$urls = Get-Content "C:\PhishGuard\openphish_urls.txt"
$results = @()
$apiUrl = "http://localhost:5204/predict"

foreach ($url in $urls) {
    if ([string]::IsNullOrWhiteSpace($url)) { continue }
    
    Write-Host "Testing: $url" -ForegroundColor Cyan
    
    try {
        $body = @{ url = $url.Trim() } | ConvertTo-Json
        $response = Invoke-RestMethod -Uri $apiUrl -Method Post -Body $body -ContentType "application/json" -TimeoutSec 10
        
        $results += [PSCustomObject]@{
            URL = $url
            IsPhishing = $response.isPhishing
            Confidence = $response.confidence
            Explanation = $response.explanation
            Status = "Success"
        }
    }
    catch {
        $results += [PSCustomObject]@{
            URL = $url
            IsPhishing = $false
            Confidence = 0
            Explanation = "API Error"
            Status = "Failed"
        }
        Write-Warning "Failed to test: $url"
    }
}

# Export results
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$outputPath = "C:\PhishGuard\test_results_$timestamp.csv"
$results | Export-Csv $outputPath -NoTypeInformation
Write-Host "Results saved to: $outputPath" -ForegroundColor Green

# Summary
$detected = ($results | Where-Object { $_.IsPhishing -eq $true }).Count
$total = $results.Count
Write-Host "Detection Rate: $detected / $total ($([math]::Round(($detected/$total)*100, 2))%)" -ForegroundColor Yellow