---
DocumentName: Outlook & Exchange Online Setup Guide
Version: 2.0
Section: Section 5 - Messaging & Collaboration
Page: 1
Approved: true
Active: true
---
# Outlook & Microsoft 365 Exchange Online Setup & Maintenance Guide

## Overview & Architecture
Corporate electronic mail, calendars, contacts, and global address lists (GAL) are hosted on **Microsoft 365 Exchange Online**. All client connections utilize Modern Authentication (OAuth2 / MSAL) with conditional access and Multi-Factor Authentication (MFA) enforcement.

---

## Setting Up Outlook Desktop (Windows & macOS)

### 1. Windows Setup
1. Open the Windows Start menu and launch **Outlook for Microsoft 365**.
2. If launching for the first time, Outlook will prompt for your email address. Enter your primary corporate UPN:
   `firstname.lastname@company.com`
3. Click **Connect**. The Microsoft Entra ID Single Sign-On window will appear.
4. Verify your credentials, complete the Microsoft Authenticator prompt on your phone, and select *"Allow my organization to manage my device"*.
5. When prompted *"Account successfully added"*, click **Done** and allow Outlook 5 to 10 minutes to populate your mailbox and synchronize offline cache.

### 2. macOS Setup
1. Launch **Microsoft Outlook** from the Applications folder.
2. Select **Tools** -> **Accounts** -> **Add Account**.
3. Enter `user@company.com`, choose **Office 365**, and authenticate via Microsoft Entra Single Sign-On.

---

## Webmail & Mobile Device Access (Outlook on the Web & OWA)
- **Direct Webmail URL**: `https://outlook.office.com` (or `https://mail.company.com`). Accessible from any modern browser with MFA.
- **Mobile Devices (iOS & Android)**:
  - Download the official **Microsoft Outlook** application from the Apple App Store or Google Play Store.
  - Native iOS Mail or Samsung Email apps are blocked by corporate conditional access policies. Only the official Microsoft Outlook app is supported due to Intune Mobile Application Management (MAM) encryption compliance.
  - Set a 6-digit corporate PIN when prompted by Intune MAM.

---

## Mailbox Limits, Quotas & Attachment Rules
- **Mailbox Storage Capacity**: 
  - Standard User Mailbox: **100 GB** storage.
  - Auto-Expanding Online Archive: An automated archive mailbox is provisioned when your primary mailbox reaches 90 GB, providing an additional 1.5 TB of historical archive capacity.
- **Attachment Size Limit**:
  - Maximum direct file attachment size is **35 MB**.
  - For files exceeding 35 MB (up to 250 GB), upload the document to **OneDrive for Business** or **SharePoint** and attach a cloud sharing link with view/edit permissions.

---

## Troubleshooting Sync Failures & Corrupt OST Cache
If Outlook displays "Disconnected", "Trying to connect...", folders fail to sync, or error `0x8004010F` appears:

### 1. Rebuilding the Offline Storage Table (.OST) File
Rebuilding the local cache does not delete emails because all mail is safely stored in the Exchange Online cloud:
1. Exit Microsoft Outlook completely. Ensure the `OUTLOOK.EXE` process is stopped in Task Manager.
2. Press `Win + R` to open the Run window, type `%LOCALAPPDATA%\Microsoft\Outlook`, and press **Enter**.
3. Locate your mailbox cache file (named `your_email@company.com.ost`).
4. Right-click the file and rename it to `your_email@company.com.ost.old`.
5. Relaunch Outlook. Outlook will automatically generate a clean `.ost` file and download a fresh copy of your mailbox from the cloud server.

### 2. Disabling Faulty Add-ins (Outlook Safe Mode)
If Outlook crashes immediately upon startup:
1. Press `Win + R`, type `outlook.exe /safe`, and click **OK**.
2. If Outlook opens normally in Safe Mode, click **File** -> **Options** -> **Add-ins**.
3. At the bottom, select **COM Add-ins** and click **Go**.
4. Uncheck third-party add-ins (e.g. Adobe PDFMaker, Zoom plugin, CRM sync tools) and restart Outlook normally.

---

## Shared Mailboxes & Calendar Delegation
- **Automatic Mapping**: Shared mailboxes to which you have been granted `FullAccess` permissions in Entra ID automatically appear in your Outlook left folder pane within 60 minutes.
- **Manual Addition**:
  1. In Outlook, click **File** -> **Account Settings** -> **Account Settings**.
  2. Select your account and click **Change** -> **More Settings** -> **Advanced** tab.
  3. Under *Open these additional mailboxes*, click **Add** and enter the shared mailbox name (e.g., `marketing-ops@company.com`).
  4. Click **Apply** and restart Outlook.
