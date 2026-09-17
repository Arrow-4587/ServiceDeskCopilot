---
DocumentName: Remote Work Security & BitLocker Guidelines
Version: 2.0
Section: Section 9 - Remote Work Safety
Page: 1
Approved: true
Active: true
---
# Remote Work Security, BitLocker Encryption & Device Hygiene Guidelines

## Overview & Scope
As a distributed enterprise, remote and hybrid employees access sensitive company networks, source code, and customer data from home offices, co-working spaces, and travel locations. This document outlines mandatory technical controls, encryption policies, and behavioral guidelines required to protect corporate endpoints.

---

## Mandatory Endpoint Encryption: BitLocker & FileVault
Every corporate laptop—without exception—must maintain full-disk hardware encryption enforced by Microsoft Intune Mobile Device Management:
- **Windows Devices**: Encrypted via **BitLocker** using XTS-AES 256-bit encryption with TPM (Trusted Platform Module) 2.0 chip binding.
- **macOS Devices**: Encrypted via **FileVault 2** with corporate recovery keys escrowed into Intune.
- **Enforcement Mechanism**: If BitLocker is disabled or tampered with, Intune conditional access automatically blocks the device from accessing email, Teams, SharePoint, and VPN until encryption is restored.

---

## Retrieving a BitLocker Recovery Key

### Scenario: The Blue BitLocker Recovery Screen
During system startup, Windows may display a blue screen asking: *"Enter the recovery key for this drive"*. This typically occurs following a BIOS firmware update, docking station hardware change, or multiple incorrect startup PIN attempts.

### Method 1: Self-Service Retrieval via Smartphone (Fastest)
1. Using your personal smartphone or tablet, open a web browser and navigate to:
   `https://myaccount.microsoft.com/device-list`
2. Sign in using your corporate email (`user@company.com`) and complete Microsoft Authenticator MFA.
3. Locate your laptop name in the device list and click **View BitLocker Keys**.
4. Match the **Key ID** shown on your phone with the first 8 characters of the Recovery Key ID displayed on your laptop screen.
5. Type the 48-digit numerical recovery key into your laptop and press **Enter** to boot into Windows.

### Method 2: Contacting the IT Service Desk
If you do not have an alternate mobile device:
1. Contact the IT Service Desk phone hotline: `+1-800-555-IT4U`.
2. Provide your employee ID and the **Recovery Key ID** (e.g. `3F8A4B2C...`) displayed on your laptop screen.
3. Once your identity is verified, the analyst will read out the 48-digit recovery key.

---

## Physical Security & Clean Desk Standards for Remote Work
- **Automatic Screen Lock**: All laptops are configured via group policy to lock after **5 minutes** of inactivity. Manually lock your workstation anytime you step away using `Win + L` (Windows) or `Ctrl + Cmd + Q` (Mac).
- **Public & Shared Spaces**:
  - Never leave a laptop unattended in coffee shops, airport waiting areas, hotel lobbies, or client conference rooms.
  - Position screens away from public view or use a 3M polarized privacy filter when working on airplanes or trains to prevent shoulder surfing.
  - Do not leave laptops in vehicles. If transport is unavoidable, lock the device in the car trunk prior to arriving at your parking destination.
- **Family & Household Members**: Corporate devices are strictly for company business. Family members, roommates, and guests must never be permitted to browse, play games, or log into personal accounts on corporate machines.

---

## Home Wi-Fi & Network Security Hygiene
- **Wi-Fi Encryption Standard**: Home wireless networks must utilize **WPA2-AES** or **WPA3-Personal** encryption. Never use unencrypted open networks or obsolete WEP/WPA-TKIP protocols.
- **Broadband Router Administration**: Change the default factory administrator password on your home Wi-Fi router. Disable remote management features on the router WAN interface.
- **Public Wi-Fi Precaution**: When connecting to airport, hotel, or public café Wi-Fi, you must immediately connect to the **Cisco AnyConnect Corporate VPN** before opening any web browser, email, or internal tool.

---

## Removable Storage Media & USB Restrictions
- **Read-Only USB Policy**: USB mass storage devices (flash drives, external hard drives, memory cards) are configured as **Read-Only** by default to prevent data leakage and USB-borne malware.
- **Transferring Large Files**: Use corporate **OneDrive for Business** or **SharePoint Online** for cloud file sharing.
- **Temporary Exemption**: If you must write data to an encrypted external drive for a client deployment, submit a temporary USB Write Permission request via the IT Portal (requires Line Manager approval).
