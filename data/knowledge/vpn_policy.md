---
DocumentName: Corporate Cisco AnyConnect VPN Policy
Version: 1.0
Section: Section 1 - Network Connectivity
Page: 1
Approved: true
Active: true
---
# Corporate Cisco AnyConnect VPN Policy

## Overview
All remote employees connecting to corporate resources (Intranet, File Shares, Internal APIs) must connect via Cisco AnyConnect VPN.

## Standard Connection Steps
1. Open Cisco AnyConnect Secure Mobility Client.
2. Enter server address: `vpn.company.com`.
3. Enter your corporate email address and network password.
4. Complete Microsoft Authenticator MFA prompt on your mobile device.

## Common Troubleshooting
- Error 403 / Access Denied: Verify your Active Directory password has not expired.
- Connection Timeout: Check local internet router and ensure port 443 (HTTPS) is open.
