# 🔒 PhishGuard  
*A Forensically Sound, Explainable Browser Extension for Real-Time Phishing Detection*

![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Chrome Extension](https://img.shields.io/badge/Manifest_V3-Chrome_Extension-4285F4?logo=google-chrome)
![License](https://img.shields.io/badge/License-Private-red)

PhishGuard is a client-side, privacy-preserving phishing detection system developed as part of a BSc Cyber Security and Forensic Science dissertation at Anglia Ruskin University. Unlike cloud-based tools, PhishGuard operates entirely **locally**, ensuring GDPR compliance, zero data exfiltration, and forensic auditability.

## ✨ Key Features

- **100% Client-Side**: No external API calls — all processing happens on-device
- **Explainable Alerts**: Human-readable reasoning (e.g., *"Spoofed brand: 'micosoft' + risky TLD: .xyz"*)
- **Hybrid Detection**: Combines ML.NET machine learning with rule-based heuristics
- **Forensic Ready**: Git-versioned models, deterministic datasets, and full audit logs
- **Ethical Design**: Uses only synthetic data — no real phishing URLs or malware

## 🏗️ Architecture

- **Backend**: ASP.NET Core Minimal API (`http://localhost:5204/predict`)
- **ML Engine**: ML.NET FastTree classifier trained on 500 synthetic URLs
- **Extension**: Manifest V3 Chrome extension with content script injection
- **Storage**: Ephemeral only — no persistent data beyond tab session

## 🧪 Evaluation Results

| Metric               | Score     |
|----------------------|-----------|
| Accuracy             | 100.00%   |
| Precision            | 100.00%   |
| Recall               | 100.00%   |
| False Positive Rate  | 0.00%     |
| Avg. Detection Time  | < 300ms   |

Validated against:
- Homograph attacks (`m1crosoft.com`, `faceb00k.net`)
- Rare TLD abuse (`.xyz`, `.gq`, `.zulu`)
- Keyword omission (`paypa1.com` with no `/login`)
- Legitimate sites (`github.com`, `slack.com`, `zoom.us`)

## 🚀 How to Run

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Google Chrome

### 1. Start the API
```powershell
cd PhishGuard.Api
dotnet run

2. Load Chrome Extension
Open chrome://extensions
Enable Developer mode
Click Load unpacked → select PhishGuard.Extension/

3. Test It
Visit locally hosted pages:
http://localhost:5204/micosoft-login.xyz.html → Phishing alert
http://localhost:5204/github.com.html → No alert


📁 Project Structure

├── PhishGuard.Api/          # Local prediction API (.NET 8)
├── PhishGuard.Trainer/      # Synthetic dataset + ML model trainer
├── PhishGuard.Tests/        # Automated 500-URL evaluation suite
├── PhishGuard.Extension/    # Chrome extension (Manifest V3)
└── wwwroot/                 # Test pages (phishing & legitimate)

