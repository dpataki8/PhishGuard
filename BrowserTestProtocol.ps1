# REAL-TIME BROWSER INTEGRATION TEST PROTOCOL
# Input: Locally hosted phishing pages (e.g., http://localhost:5204/micosoft-login.xyz.html)
# Environment: Chrome Incognito mode (avoids cache/extension conflicts)
# Metrics: 
#   - Time-to-alert (measured via service worker timestamps)
#   - Banner visibility (screenshot validation)
#   - User dismissibility (manual interaction test)
# Evidence: Service worker logs + page screenshots