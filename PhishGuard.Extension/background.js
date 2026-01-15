// Log that the background script has loaded
console.log("[PhishGuard] Background script loaded and listening for navigation events.");

// Listen for committed navigation events
chrome.webNavigation.onCommitted.addListener((details) => {
  // Only process main frame navigations (ignore iframes)
  if (details.frameId !== 0) return;

  // Only process HTTP/HTTPS URLs
  if (!details.url.startsWith('http')) return;

  console.log("[PhishGuard] Analyzing URL:", details.url);

  // Send URL to local API
  fetch("http://localhost:5204/predict", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ url: details.url })
  })
  .then(response => {
    if (!response.ok) throw new Error(`API status: ${response.status}`);
    return response.json();
  })
  .then(result => {
    console.log("[PhishGuard] API Response:", result);

    if (result.isPhishing) {
      // 1. Send alert to content script (for in-page banner)
      chrome.tabs.sendMessage(details.tabId, {
        type: "SHOW_PHISHING_ALERT",
        explanation: result.explanation
      });

      // 2. SET RED BADGE ON EXTENSION ICON
      chrome.action.setBadgeText({ tabId: details.tabId, text: "⚠️" });
      chrome.action.setBadgeBackgroundColor({ tabId: details.tabId, color: "#d32f2f" });
    } else {
      // Clear badge if safe
      chrome.action.setBadgeText({ tabId: details.tabId, text: "" });
    }
  })
  .catch(error => {
    console.warn("[PhishGuard] API error:", error.message);
    // Clear badge on error
    chrome.action.setBadgeText({ tabId: details.tabId, text: "" });
  });
}, { url: [{ schemes: ["http", "https"] }] });