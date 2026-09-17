---
DocumentName: Corporate Wi-Fi Access Guide
Version: 2.0
Section: Section 3 - Wireless Access
Page: 1
Approved: true
Active: true
---
# Corporate Wi-Fi Network Access, Roaming & Diagnostics Guide

## Campus Wireless Infrastructure & SSID Topology
All corporate buildings, regional headquarters, technology labs, and branch offices broadcast standardized wireless network identifiers (SSIDs) powered by enterprise Cisco Catalyst / Aruba Wi-Fi 6E access points:

### 1. `Company-Corp` (Secure Enterprise Network)
- **Target Audience**: Corporate-managed laptops, tablets, and mobile devices enrolled in Microsoft Intune.
- **Security Protocol**: WPA2-Enterprise / WPA3-Enterprise using **802.1X EAP-TLS** (Machine Certificate) and **PEAP-MSCHAPv2** (User Credentials).
- **Access**: Full access to corporate internal IP subnets, intranet portals, network file shares, and local printers without requiring VPN.

### 2. `Company-Guest` (Visitor & Personal Devices)
- **Target Audience**: Clients, contractors, vendors, and employee personal smartphones (BYOD).
- **Security Protocol**: Captive Portal with web-based sponsorship or self-registration.
- **Access**: Internet access only. Complete network isolation from corporate intranet subnets. Bandwidth capped at 25 Mbps per client.

### 3. `Company-IoT` (Smart Building & Conference Devices)
- **Target Audience**: Zoom Room / Teams Room consoles, smart meeting TVs, Apple TVs, networked digital signage.
- **Security Protocol**: WPA2-PSK with centralized MAC address whitelisting managed by Network Operations.

---

## Connecting to `Company-Corp` (Step-by-Step)

### Windows 11 Laptops (Automated Intune Deployment)
1. On corporate-managed Windows laptops, the `Company-Corp` wireless profile is automatically deployed via Microsoft Intune.
2. Click the Network icon in the Windows taskbar system tray.
3. Select `Company-Corp` and check **Connect automatically**.
4. If prompted to sign in, ensure *"Use my Windows user account"* is checked, or type your corporate email address (`user@company.com`) and network password.
5. If prompted with *"Continue connecting? The server presented a certificate Corp-CA-01"*, verify the thumbprint and click **Connect**.

### macOS Laptops
1. Click the Wi-Fi icon in the macOS menu bar and select `Company-Corp`.
2. Enter your corporate username (`firstname.lastname`) and your corporate password.
3. When prompted to trust the root authority certificate `Corp-Issuing-CA`, click **Continue** and enter your macOS administrator Touch ID / password.

---

## Visitor & Guest Wi-Fi Access (`Company-Guest`)
Visitors requiring internet connectivity while at corporate facilities:
1. Select the Wi-Fi network `Company-Guest` on your laptop or mobile phone.
2. A captive web portal will launch automatically (if it does not appear, open a browser and navigate to `http://guestwifi.company.com`).
3. **Choose Registration Method**:
   - **Self-Registration (SMS Voucher)**: Enter your name, mobile phone number, and company name. An SMS containing a 6-digit access code is sent immediately. Valid for **24 hours**.
   - **Employee Sponsor**: Enter the corporate email of your meeting host (e.g., `host.name@company.com`). An automated approval prompt is sent to the employee's Teams/Outlook client. Upon approval, access is granted for **7 days**.

---

## Wireless Troubleshooting & Diagnostics

### 1. Fix: Self-Assigned / APIPA IP Address (169.254.x.x)
If your laptop connects to `Company-Corp` but shows "No Internet, secured" or fails to receive an IP address from the DHCP server:
1. Open Command Prompt as administrator.
2. Release and renew your local DHCP network lease:
   `ipconfig /release`
   `ipconfig /renew`
3. Flush DNS resolver cache:
   `ipconfig /flushdns`
4. Confirm you received a valid corporate IP address (e.g. `10.140.x.x`).

### 2. Fix: Continuous Disconnections While Walking Across Office Floors (Roaming Drops)
If your Wi-Fi disconnects when moving between conference rooms or floors:
1. Press `Win + X` and select **Device Manager**.
2. Expand **Network adapters**, right-click your Wi-Fi adapter (e.g., *Intel Wi-Fi 6E AX211*), and select **Properties**.
3. Go to the **Advanced** tab.
4. Locate **Roaming Aggressiveness** and change the value to **Medium-Low** or **Medium**. (High aggressiveness causes the card to excessively disconnect when scanning for marginal signal improvements).
5. Click **OK** and restart Wi-Fi.

### 3. Expired Machine Certificate or Broken Wi-Fi Profile
If `Company-Corp` rejects your credentials:
1. Connect your laptop temporarily to an office docking station with an Ethernet cable.
2. Open the **Company Portal** app.
3. Go to **Settings** (gear icon) and click **Sync** under Device sync status to download the latest Intune wireless certificate from `Corp-CA-01`.
