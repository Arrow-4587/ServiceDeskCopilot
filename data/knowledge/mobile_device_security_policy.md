---
DocumentName: Corporate Mobile Device Security & BYOD Policy
Version: 1.0
Section: Section 8 - Mobile Security
Page: 1
Approved: true
Active: true
---
# Corporate Mobile Device Security, Enrollment & BYOD Policy

## Overview & Scope
This policy defines the security requirements for corporate-owned and personally owned mobile devices that access company email, collaboration tools, cloud storage, internal applications, or other corporate data.

---

## Approved Device Requirements
Mobile devices must meet the following minimum requirements before access is granted:
- **Supported Platforms**: Apple iOS/iPadOS 17 or later, and Android 13 or later.
- **Device Security**: A PIN, password, biometric lock, or other approved device-lock method must be enabled.
- **Operating System Updates**: Devices must install critical security updates within 14 days of release.
- **Encryption**: Native device encryption must remain enabled.
- **Prohibited States**: Rooted, jailbroken, or bootloader-unlocked devices are not permitted.

---

## Corporate-Owned Device Enrollment
All corporate-issued mobile devices must be enrolled in Microsoft Intune before they are issued to an employee:

1. Power on the device and connect to a trusted Wi-Fi or mobile network.
2. Sign in with your corporate email address and password.
3. Complete Microsoft Authenticator multi-factor authentication when prompted.
4. Accept the Company Portal enrollment and compliance policies.
5. Confirm that the device appears as compliant in the Company Portal application.

Devices that fail compliance checks may have access to corporate email, Teams, OneDrive, and internal applications restricted automatically.

---

## Bring Your Own Device (BYOD) Access
Employees may use personally owned devices for approved business applications when enrolled through the corporate mobile application management profile:

- Only approved corporate applications may access business data.
- Corporate data must remain within managed applications.
- Copying corporate data to personal applications, personal cloud storage, or unsecured messaging platforms is prohibited.
- The organization may remove corporate application data from a personal device when access is revoked, without deleting personal photos, messages, or files.
- Employees must report a change of device ownership or phone number to the IT Service Desk.

---

## Lost, Stolen, or Compromised Devices
If a mobile device is lost, stolen, or suspected to be compromised:

1. Immediately contact the IT Service Desk or Security Operations Center.
2. Change your corporate password from a trusted device.
3. Revoke active sessions through the corporate security portal where available.
4. Do not attempt to access corporate resources from the missing device.
5. IT may remotely remove corporate data or fully wipe corporate-owned devices to protect company information.

Lost or stolen devices must be reported within **one hour** of discovery.

---

## Prohibited Mobile Device Activities
The following activities are prohibited on any device accessing corporate resources:
- Sharing corporate credentials or MFA approval requests with another person.
- Storing company documents in personal cloud-storage applications.
- Using unapproved screen-recording, remote-access, or file-sharing applications.
- Disabling device encryption, screen lock, endpoint protection, or management profiles.
- Connecting to corporate resources through rooted, jailbroken, or otherwise modified devices.

---

## Support & Escalation
For enrollment, compliance, or access issues:
- Open an incident through the IT Service Desk Portal.
- Select the category **Mobile Device > Enrollment and Compliance**.
- For urgent security concerns, contact the Security Operations Center at `soc@company.com`.
