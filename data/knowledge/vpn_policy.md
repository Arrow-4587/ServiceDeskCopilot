---
DocumentName: Corporate Cisco AnyConnect VPN Policy
Version: 2.1
Section: Section 1 - Network Connectivity
Page: 1
Approved: true
Active: true
---
# Corporate Cisco AnyConnect VPN Policy & Troubleshooting Guide

## Overview & Architecture
The Corporate Virtual Private Network (VPN) is powered by the Cisco AnyConnect / Cisco Secure Client architecture. It provides an encrypted, authenticated tunnel connecting remote employees securely to corporate intranet resources, on-premises data centers, private cloud subnets, and internal API gateways.

### Split Tunneling vs Full Tunneling
- **Split Tunneling (Standard Remote Profile)**: By default, corporate traffic directed toward internal IP address ranges (`10.0.0.0/8`, `172.16.0.0/12`, and `192.168.0.0/16`) is encrypted and routed through the secure enterprise tunnel. General public internet traffic (such as Microsoft 365, YouTube, video conferencing streams, and public browsing) is routed directly through your local ISP to optimize bandwidth and minimize latency.
- **Full Tunneling (High Security Profile)**: Mandatory for employees handling sensitive customer financial records, healthcare data, or accessing isolated production environments. All outbound internet traffic is backhauled through the enterprise firewall cluster.

---

## Standard Connection Steps
1. **Launch Client**: Open the **Cisco AnyConnect Secure Mobility Client** (or **Cisco Secure Client**) from the Windows Start menu or macOS Applications folder.
2. **Select Gateway Server**:
   - Primary Gateway: `vpn.company.com`
   - Secondary / Disaster Recovery Gateway: `vpn-backup.company.com`
   - Asia-Pacific Gateway: `vpn-apac.company.com`
   - Europe Gateway: `vpn-eu.company.com`
3. **Authenticate**:
   - Enter your primary corporate email address (UPN format: `firstname.lastname@company.com`).
   - Enter your Active Directory / Entra ID network password.
4. **Complete MFA Verification**:
   - A 2-digit number matching prompt will appear on your registered **Microsoft Authenticator** mobile app.
   - Enter the matching number on your phone and tap **Approve**.
5. **Connection Banner**: Once connected, the AnyConnect icon in the Windows System Tray (or macOS Menu Bar) will display a closed padlock icon indicating an active, encrypted tunnel.

---

## Technical Prerequisites & Network Requirements
- **Client Software**: Cisco AnyConnect Secure Mobility Client version `4.10.x` or Cisco Secure Client version `5.x` or higher.
- **Operating Systems**: Windows 11 Enterprise (64-bit), Windows 10 (22H2+), macOS Sonoma (14.x) or Sequoia (15.x), and Ubuntu LTS 22.04/24.04 (corporate build).
- **Firewall Ports & Protocols**:
  - **TCP Port 443 (TLS)**: Primary control and data channel fallback.
  - **UDP Port 443 / UDP Port 10000 (DTLS / IPsec)**: High-speed datagram transport channel. If DTLS is blocked by local hotel/ISP firewalls, the client automatically falls back to TLS on port 443.
- **Device Compliance**: The device must have an active machine certificate issued by `Corp-CA-01` and pass Microsoft Intune compliance checks (BitLocker enabled, antivirus signatures up to date).

---

## Common Error Codes & Troubleshooting Workflows

### 1. Error: "Login Failed: Access Denied / Error 403"
- **Root Cause**: Your corporate password has expired, your account is temporarily locked due to failed attempts, or conditional access detected an untrusted location.
- **Resolution**:
  1. Visit the Self-Service Password Reset portal at `https://passwordreset.microsoftonline.com` to confirm account status.
  2. If your account is locked, wait 15 minutes or perform an SSPR unlock.
  3. Verify that you have completed the Authenticator push notification within the 60-second timeout window.

### 2. Error: "Connection Attempt Has Timed Out"
- **Root Cause**: Local ISP router is blocking IPsec/DTLS packets or DNS resolution for `vpn.company.com` failed.
- **Resolution**:
  1. Open Command Prompt and run `nslookup vpn.company.com` to verify DNS resolution.
  2. Switch connection from Wi-Fi to a wired Ethernet cable or test via your mobile phone 5G hotspot.
  3. Restart your home internet router/modem to clear stuck NAT translation tables.

### 3. Error: "MTU Packet Drop / High Latency on Home Broadband"
- **Root Cause**: Certain home cable and fiber ISPs encapsulate packets with PPPoE or DS-Lite, causing packet fragmentation over the VPN adapter.
- **Resolution**: Adjust the VPN adapter MTU to 1300 bytes:
  - Run Command Prompt as Administrator:
    `netsh interface ipv4 set subinterface "Cisco AnyConnect Secure Mobility Client" mtu=1300 store=persistent`

### 4. Client Software Reinstallation & Clean Cache Reset
If the Cisco AnyConnect client fails to initialize or displays "Virtual Adapter Driver Error":
1. Open Windows **Services** (`services.msc`) and restart the **Cisco AnyConnect Secure Mobility Agent** service.
2. If corrupt profile files exist, close AnyConnect, open File Explorer, and delete the cache:
   `%PROGRAMDATA%\Cisco\Cisco AnyConnect Secure Mobility Client\Profile\`
3. Relaunch AnyConnect and enter `vpn.company.com`.

---

## Escalation Matrix & Support Contacts
If connection issues cannot be resolved through the steps above:
- **IT Service Desk Chat**: Open the floating Copilot widget on the dashboard to generate an Incident Draft.
- **Urgent Network Escalation**: Contact the 24/7 Global Network Operations Center (NOC) at `noc-dispatch@company.com` or call extension `4357` (HELP).
