document.getElementById('testBtn').addEventListener('click', async () => {
  const button = document.getElementById('testBtn');
  const resultDiv = document.getElementById('result');
  
  // Disable button during request
  button.disabled = true;
  button.textContent = "Testing...";
  resultDiv.style.display = "none";

  try {
    // Use the same API URL as background.js
    const apiUrl = "https://localhost:7204/predict"; // ← Change to 7123 if using VS native
    
    const response = await fetch(apiUrl, {
      method: "POST",
      mode: "cors",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ url: "http://micosoft-login.xyz/" })
    });

    if (!response.ok) throw new Error(`API returned ${response.status}`);

    const result = await response.json();

    // Show result
    resultDiv.className = result.isPhishing ? "phishing" : "safe";
    resultDiv.innerHTML = `
      <strong>${result.isPhishing ? '🚨 PHISHING DETECTED' : '✅ SAFE'}</strong><br>
      ${result.explanation}<br>
      <small>Confidence: ${(result.confidence * 100).toFixed(1)}%</small>
    `;
    resultDiv.style.display = "block";

  } catch (error) {
    console.error("Test failed:", error);
    resultDiv.className = "error";
    resultDiv.innerHTML = `❌ Test failed: ${error.message}.<br>Is your API running?`;
    resultDiv.style.display = "block";
  } finally {
    button.disabled = false;
    button.textContent = "🔍 Test Detection";
  }
});