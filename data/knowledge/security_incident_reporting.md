---
DocumentName: Phishing & Security Incident Reporting Protocol
Version: 2.0
Section: Section 8 - Cyber Security
Page: 1
Approved: true
Active: true
---
# Phishing & Cyber Security Incident Reporting Protocol

## Overview & Policy Mandate
Information security is a collective responsibility across all enterprise employees, contractors, and partners. This protocol governs the immediate reporting, containment, and response procedures for suspected social engineering, phishing, ransomware, credential compromise, or physical loss of corporate data assets.

---

## Recognizing Phishing & Social Engineering Attacks
Cyber criminals frequently impersonate internal executives, trusted vendors, or cloud services. Key indicators of malicious communication include:
- **Urgency & Coercion**: Demands for immediate action ("Account suspended within 1 hour", "Urgent wire transfer", "CEO requests gift card purchase").
- **Spoofed Sender Addresses**: Discrepancies between the display name and actual email domain (e.g., `ceo@company-secure-portal.com` instead of `@company.com`).
- **Suspicious Attachments**: Unsolicited `.zip`, `.iso`, `.html`, `.exe`, or macro-enabled `.xlsm` files.
- **QR Code Phishing (Quishing)**: Emails instructing users to scan a QR code using a personal mobile phone to bypass corporate secure email gateways.

---

## Reporting Suspicious Emails (Step-by-Step)

### Method 1: The "Report Phishing" Ribbon Button (Preferred)
1. Select or open the suspicious email in Microsoft Outlook (Desktop, Web, or Mobile).
2. Click the **Report Phishing** (or **PhishAlarm**) button located on the top Outlook ribbon.
3. Confirm submission. The message is automatically moved to your Deleted Items folder and encrypted copies with full header metadata are submitted to the automated threat analysis sandbox.

### Method 2: Manual Forwarding with Full Internet Headers
If the reporting button is unavailable:
1. Create a new email addressed to:
   - Primary: `phishing@company.com`
   - Secondary: `soc@company.com`
2. Drag and drop the suspicious email into the new message window so it attaches as an **RFC 822 (`.eml`) attachment** (do not simply click "Forward", as headers are stripped).
3. Set the subject line to: `[SUSPECTED PHISH] - <Original Subject>`.
4. Send the message.

---

## Emergency Protocol: Ransomware, Malware & Active Compromise
If your computer screen locks with a ransom demand, files begin appending strange extensions (e.g., `.locked`, `.crypto`), or you inadvertently clicked a malicious link and entered corporate credentials:

### Immediate 4-Step Containment Protocol
1. **Sever Network Connections Immediately**:
   - Unplug physical Ethernet network cables from your laptop/docking station.
   - Disable Wi-Fi and Bluetooth immediately using the hardware switch or Windows Quick Settings.
2. **DO NOT Power Off or Reboot**:
   - Leave the machine powered on and idle. Turning off the device can destroy volatile RAM memory evidence and forensic artifacts critical for the DFIR (Digital Forensics & Incident Response) team.
3. **Notify Security Operations Center (SOC) Immediately**:
   - **Emergency 24/7 Hotline**: `1-800-SEC-HELP` (`1-800-732-4357`) or `+1-555-0199`.
   - **Internal Extension**: `9911` from any office VoIP phone.
4. **Account Quarantine**:
   - From an alternate device (e.g. mobile phone), visit `https://passwordreset.microsoftonline.com` and change your corporate password to revoke active OAuth refresh tokens.

---

## Lost or Stolen Corporate Laptops & Mobile Devices
If a corporate laptop, smartphone, or hardware security token is lost, stolen, or misplaced:
1. **Mandatory Reporting Window**: You must report the incident to the IT Service Desk within **two (2) hours** of discovering the loss.
2. **Incident Dispatch**: The Service Desk initiates an emergency incident ticket and escalates to the SecOps On-Call Engineer.
3. **Remote Wipe Execution**: The SOC dispatches a cryptographic remote wipe command via Microsoft Intune to erase all enterprise data, BitLocker keys, and cached credentials upon next network contact.
4. **Police Report Requirement**: If stolen in a public area or home burglary, obtain a local police incident report number and provide it to Corporate Facilities & Legal.

---

## Security Incident Severity Classification & SLA Response
- **Severity 1 (Critical)**: Active ransomware spread, data exfiltration, production infrastructure compromise. SOC response SLA: **< 15 minutes**.
- **Severity 2 (High)**: Confirmed credential compromise, stolen device with sensitive data, targeted spear-phishing click. SOC response SLA: **< 1 hour**.
- **Severity 3 (Medium)**: Isolated malware blocked by Defender, spam campaign, lost phone with remote wipe completed. SOC response SLA: **< 4 hours**.
- **Severity 4 (Low)**: General phishing report, false positive security notification. SOC response SLA: **< 24 hours**.
