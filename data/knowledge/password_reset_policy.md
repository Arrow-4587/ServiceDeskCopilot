---
DocumentName: Corporate Password Reset Policy
Version: 2.0
Section: Section 2 - Identity & Access
Page: 1
Approved: true
Active: true
---
# Corporate Password Reset & Identity Access Policy

## Overview
This policy defines the security standards, complexity rules, expiration cycles, and self-service procedures for corporate Active Directory and Microsoft Entra ID user accounts across all business units.

---

## Password Complexity Requirements
All corporate user account passwords must satisfy the following minimum criteria enforced by Entra ID Password Protection:
- **Minimum Length**: At least 14 characters in length (16+ characters recommended for administrative accounts).
- **Character Diversity**: Must contain characters from at least three (3) of the following four (4) categories:
  1. English uppercase letters (`A` through `Z`)
  2. English lowercase letters (`a` through `z`)
  3. Base 10 digits (`0` through `9`)
  4. Non-alphanumeric special symbols (`!`, `@`, `#`, `$`, `%`, `^`, `&`, `*`, `(`, `)`, `-`, `_`, `+`, `=`)
- **Prohibited Patterns**:
  - Cannot contain the user's account name, first name, last name, or department.
  - Cannot contain common dictionary words, company brand names, or seasonal terms (e.g., `Company2026!`, `Summer2026!`).
  - Cannot match any previously breached password tracked in global threat intelligence lists.

---

## Lifecycle, Rotation & Expiration Guidelines
- **Expiration Cycle**: Standard user passwords expire every **90 calendar days**.
- **Advance Notifications**: Automated warning notifications are dispatched to the user's primary corporate inbox at **14 days, 7 days, 3 days, and 1 day** prior to expiration.
- **History Limitation**: Users cannot reuse any of their previous **10 passwords**.
- **Immediate Expiration on Compromise**: If an account is flagged by Microsoft Entra ID Identity Protection with "High Risk" or "Leaked Credentials", the password is automatically invalidated, requiring immediate reset.

---

## Account Lockout Protocol
- **Lockout Threshold**: Entering an incorrect password **5 consecutive times** will trigger an automatic account lockout.
- **Lockout Duration**: Accounts remain locked for **15 minutes**, after which the lockout counter automatically resets.
- **Immediate Self-Service Unlock**: Users do not need to wait 15 minutes; they can immediately unlock their account via the Self-Service Password Reset (SSPR) portal.

---

## Self-Service Password Reset (SSPR) Step-by-Step
Employees are required to register for SSPR upon onboarding. To reset a forgotten password or unlock your account:

1. **Access Portal**: Open a web browser on any internet-connected device (including mobile) and visit:
   `https://passwordreset.microsoftonline.com` (or the shortcut `https://passwordreset.company.com`).
2. **Enter Identity**: Input your full corporate email address (`user@company.com`) and complete the CAPTCHA verification challenge.
3. **Verify Identity (MFA)**: Complete two of your registered authentication methods:
   - Method 1: Approve notification on the **Microsoft Authenticator** app or enter the 6-digit one-time code.
   - Method 2: Enter the 6-digit SMS verification code received on your registered mobile number.
4. **Choose New Password**: Enter your new password meeting the 14-character complexity requirements and re-enter to confirm.
5. **Confirmation**: A confirmation screen and an alert email will confirm that your password was successfully updated across Active Directory, Microsoft 365, VPN, and corporate SSO.

---

## Updating Cached Credentials After Password Reset
After changing your corporate password, update cached credentials on all connected devices to avoid triggering false account lockouts:
1. **Corporate Laptop**: If working remotely, connect to Cisco AnyConnect VPN using your new password, then press `Ctrl + Alt + Delete` and click **Lock**, then unlock using your new password to update your local Windows credential cache.
2. **Mobile Devices**: Open Outlook and Teams on your iOS/Android phone; tap the prompt to re-authenticate with your new password.
3. **Office Wi-Fi**: Forget and reconnect to the `Company-Corp` Wi-Fi network using your updated password.

---

## Emergency Support & Temporary Access Pass (TAP)
If an employee loses their mobile phone or cannot complete SSPR:
- Contact the IT Service Desk by phone: `+1-800-555-IT4U` or visit the local campus IT Walk-Up Service Bar.
- The analyst will verify employee identity via video call (HR employee ID and government photo ID check).
- The analyst can issue a **Temporary Access Pass (TAP)**—a one-time, time-limited passcode valid for 8 hours allowing the user to sign in and register a new Authenticator device.
