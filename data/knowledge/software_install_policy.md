---
DocumentName: Approved Software Request & Catalog Policy
Version: 2.0
Section: Section 7 - Software Management
Page: 1
Approved: true
Active: true
---
# Approved Software Request, Self-Service Catalog & Admin Rights Policy

## Overview & Principle of Least Privilege
To safeguard enterprise assets against malware, ransomware, unauthorized software vulnerabilities, and license non-compliance, all employee workstations operate under the **Principle of Least Privilege**. Corporate user accounts do not possess permanent local administrator privileges.

---

## Self-Service Software Center & Company Portal
Employees can install verified, pre-approved software packages on demand without requiring IT intervention or administrator credentials:

### How to Access Company Portal
1. Click the Windows Start menu or press the Windows Key.
2. Search for and launch **Company Portal** (or **Software Center** on legacy machines).
3. Browse the **Applications** tab by category (Developer Tools, Productivity, Communication, Design).
4. Click on the desired software and click **Install**.
5. The software is downloaded from the corporate cloud repository and silently installed in the background.

### Standard Approved Catalog Titles
The following titles are pre-approved and maintained with automated patch management:
- **Browsers**: Google Chrome Enterprise, Mozilla Firefox ESR, Microsoft Edge.
- **Developer Tools**: Visual Studio Code, Git for Windows, Node.js LTS, Python 3.11+, JetBrains Toolbox, Windows Terminal.
- **Containers & Virtualization**: Docker Desktop (requires corporate license assignment), WSL2 (Windows Subsystem for Linux).
- **Communication & Collaboration**: Microsoft Teams, Slack, Zoom Client, Miro Desktop, Notion.
- **Productivity & Utilities**: Microsoft 365 Apps, Power BI Desktop, Adobe Acrobat Reader DC, 7-Zip, Notepad++.

---

## Requesting Non-Standard Commercial Software
If your job role requires commercial software that is not present in the Company Portal catalog:
1. Navigate to the **IT Service Desk Portal** and choose **Request Non-Standard Software**.
2. Complete the request form detailing:
   - Software Name and Vendor.
   - Specific Business Justification (describe workflows that cannot be completed with standard tools).
   - Quantity of Licenses Required.
   - Billing Cost Center (Department budget code).
3. **Approval Workflow**:
   - Level 1: Direct Line Manager cost-center budget approval.
   - Level 2: Software Architecture Review Board (SARB) & Information Security review for license compliance, data privacy, and third-party security risk assessment.
4. **Provisioning**: Once approved, the software is packaged into Intune and made available in your Company Portal within 2 business days.

---

## Temporary Local Administrator Rights (Just-In-Time Elevation)
For software developers, system engineers, or lab researchers who require temporary elevated rights for compilation, driver testing, or local registry configuration:
- **MakeMeAdmin / Azure PIM Elevation**:
  1. Open the system tray, right-click the **MakeMeAdmin** icon, and select **Request Administrator Rights**.
  2. Enter your business justification (e.g., "Docker daemon setup and local network binding").
  3. Your user account is added to the local Administrators group for a maximum of **4 hours**.
  4. All elevated actions are logged and audited by the Security Operations Center (SOC).
  5. Permanent standing local admin accounts are strictly prohibited.

---

## Prohibited Software & Shadow IT Enforcement
Workstations are monitored by Microsoft Defender for Endpoint. The installation or execution of the following categories of software is strictly banned and triggers an automated security alert:
- **Unapproved Remote Access Tools**: TeamViewer, AnyDesk, LogMeIn, VNC without corporate licensing.
- **P2P & Torrent Clients**: BitTorrent, uTorrent, qBittorrent.
- **Cryptocurrency Miners**: Any cryptocurrency mining scripts or executables.
- **Hacking & Cracking Utilities**: Password dumpers, network packet sniffers without prior written InfoSec authorization.
- **Pirated or Cracked Software**: Results in immediate revocation of system access and escalation to HR disciplinary committee.
