// Listen for phishing alerts from background script
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
  if (request.type === "SHOW_PHISHING_ALERT") {
    // Avoid duplicate banners
    if (document.getElementById("phishguard-banner")) return;

    const banner = document.createElement("div");
    banner.id = "phishguard-banner";
    
    // Create banner container with inline styles
    banner.innerHTML = `
      <div id="phishguard-banner-content" style="
        position: fixed; 
        top: 0; 
        left: 0; 
        width: 100%; 
        background: #d32f2f; 
        color: white; 
        padding: 12px 16px; 
        z-index: 2147483647; 
        font-family: -apple-system, BlinkMacSystemFont, Arial, sans-serif;
        border-bottom: 2px solid #b71c1c; 
        box-shadow: 0 2px 12px rgba(0,0,0,0.25);
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
      ">
        <div style="flex: 1; margin-right: 12px;">
          <strong>🚨 PHISHING DETECTED</strong><br>
          ${request.explanation}
        </div>
        <button id="phishguard-dismiss-btn" style="
          background: white; 
          color: #d32f2f; 
          border: 1px solid white; 
          border-radius: 4px; 
          padding: 4px 10px; 
          cursor: pointer; 
          font-weight: bold;
          font-size: 14px;
          flex-shrink: 0;
          margin-top: 2px;
        ">Dismiss</button>
      </div>
    `;
    
    if (document.body) {
      // Adjust page content to avoid being hidden under banner
      document.body.style.marginTop = "60px";
      document.body.prepend(banner);

      // Add dismiss functionality
      const dismissBtn = document.getElementById("phishguard-dismiss-btn");
      if (dismissBtn) {
        dismissBtn.addEventListener("click", () => {
          // Remove banner
          const bannerEl = document.getElementById("phishguard-banner");
          if (bannerEl) bannerEl.remove();
          
          // Reset body margin
          document.body.style.marginTop = "";
          
          // Clear any session state (forensic cleanliness)
          sessionStorage.removeItem("phishguard_alert_active");
        });
      }

      // Optional: Track that alert was shown (for analytics/testing)
      sessionStorage.setItem("phishguard_alert_active", "true");
    }
  }
});