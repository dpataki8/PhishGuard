// Listen for phishing alerts from background script
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
  if (request.type === "SHOW_PHISHING_ALERT") {
    // Avoid duplicate banners
    if (document.getElementById("phishguard-banner")) return;

    const banner = document.createElement("div");
    banner.id = "phishguard-banner";
    banner.innerHTML = `
      <div style="position: fixed; top: 0; left: 0; width: 100%; 
                  background: #ffebee; color: #c62828; padding: 12px; 
                  z-index: 2147483647; font-family: -apple-system, BlinkMacSystemFont, sans-serif;
                  border-bottom: 2px solid #ef9a9a; box-shadow: 0 2px 8px rgba(0,0,0,0.1);">
        <strong>🚨 PHISHING DETECTED</strong><br>
        ${request.explanation}
      </div>
    `;
    
    if (document.body) {
      document.body.style.marginTop = "60px";
      document.body.prepend(banner);
    }
  }
});