---
DocumentName: Microsoft Teams & MFA Troubleshooting
Version: 2.0
Section: Section 10 - Collaboration Tools
Page: 1
Approved: true
Active: true
---
# Microsoft Teams & Multi-Factor Authentication (MFA) Guide

## Overview & Architecture
Microsoft Teams is the corporate enterprise platform for chat, video conferencing, screen sharing, and telephony. Access to Teams and all Microsoft 365 services is gated by **Multi-Factor Authentication (MFA)** managed in Microsoft Entra ID to protect user identities against credential theft and unauthorized access.

---

## Multi-Factor Authentication (MFA) Setup & Registration

### First-Time Registration Workflow
All new hires and employees adding a new mobile phone must register their security factors:
1. On a computer, navigate to the official Microsoft Security Info portal:
   `https://aka.ms/mfasetup` (or `https://mysignins.microsoft.com/security-info`).
2. Sign in with your corporate email (`firstname.lastname@company.com`) and password.
3. Click **Add sign-in method** and select **Authenticator app**.
4. Download and install **Microsoft Authenticator** on your mobile device (available on Apple App Store for iOS and Google Play Store for Android).
5. Open the app, tap the `+` icon, select **Work or school account**, and choose **Scan QR code**.
6. Point your mobile phone camera at the QR code displayed on your computer screen.
7. Complete the test verification challenge by typing the 2-digit number displayed on your computer into the mobile app prompt.
8. **Add a Backup Method**: We strongly recommend adding a secondary phone number for SMS/voice verification as a fallback in case your smartphone is lost or damaged.

---

## Number Matching & Anti-MFA Fatigue Security
To protect employees against "MFA Fatigue Attacks" (where attackers repeatedly send push notifications until a user accidentally clicks Approve):
- Corporate Entra ID enforces **Number Matching**.
- When you attempt to sign in, your browser or desktop client displays a distinct 2-digit number (e.g., `42`).
- You must enter that exact number into your Microsoft Authenticator app on your phone to complete authentication.
- If you receive a push notification when you are *not* actively signing in, tap **No, it's not me** and notify `soc@company.com` immediately.

---

## Microsoft Teams Video & Audio Diagnostics

### 1. Microphone & Speaker Device Selection
If attendees cannot hear you or you cannot hear audio during a meeting:
1. In Microsoft Teams, click the **three dots (`...`)** next to your profile picture in the top right, then select **Settings**.
2. Click **Devices** in the left sidebar.
3. Under **Audio devices**, ensure your headset (e.g., *Jabra Evolve2* or *AirPods*) is selected as both the **Speaker** and **Microphone**, rather than default laptop speakers.
4. Scroll down and click **Make a test call**. Record a 10-second message and listen to the playback to confirm audio quality.

### 2. Camera Permissions & Video Stuttering
- **Windows Privacy Permissions**: If Teams displays a black screen or "No camera found", open Windows Settings (`Win + I`) -> **Privacy & Security** -> **Camera**, and ensure **"Let desktop apps access your camera"** is toggled **ON**.
- **Hardware Shutter**: Verify the physical sliding privacy shutter on your laptop webcam is open.

### 3. Clearing Corrupt Teams Cache (High CPU / Glitches / Sync Delay)
If Teams freezes, fails to show updated messages, or consumes high memory:
1. Right-click the Microsoft Teams icon in the system tray and select **Quit**.
2. Press `Win + R` to open Run, type the following path, and press **Enter**:
   `%LOCALAPPDATA%\Packages\MSTeams_8wekyb3d8bbwe\LocalCache\Microsoft\MSTeams`
   *(For classic Teams: `%APPDATA%\Microsoft\Teams`)*
3. Select all files in the folder and press **Delete**.
4. Restart Microsoft Teams. The application will fetch clean cache data from the cloud.

---

## Lost Phone, Broken Device & Traveling Abroad Protocols

### 1. Temporary Access Pass (TAP) for Lost Mobile Devices
If your mobile phone is lost, stolen, or undergoing factory repair:
- Contact the IT Service Desk via phone: `+1-800-555-IT4U`.
- After verifying your identity via manager confirmation and employee ID, an analyst will generate an 8-hour **Temporary Access Pass (TAP)**.
- Use the TAP as a passwordless login to sign into `https://aka.ms/mfasetup` and register your new replacement phone.

### 2. International Travel & Conditional Access Notice
If you are traveling abroad for business or vacation:
- Corporate Entra ID uses geofencing to block logins from unexpected international IP ranges.
- Submit an **International Travel Notification** on the IT Service Portal at least **3 business days prior** to departure specifying your travel dates and destination countries to avoid automated travel lockout.
