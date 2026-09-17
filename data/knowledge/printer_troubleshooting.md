---
DocumentName: Network Printer Mapping & Diagnostics
Version: 2.0
Section: Section 4 - Peripherals & Hardware
Page: 1
Approved: true
Active: true
---
# Network Printer Mapping, FollowMe Secure Print & Diagnostics

## Architecture: FollowMe Secure Print
Corporate office facilities utilize a centralized **FollowMe Secure Print** architecture. Rather than sending documents directly to a specific physical printer in an open hallway, print jobs are sent to a single centralized virtual queue. Print jobs are encrypted and held securely until the employee walks up to any multi-function printer (MFP) on any floor or corporate building, taps their employee RFID badge, and releases the job.

### Key Benefits
- **Document Confidentiality**: Documents never sit unattended on open output trays.
- **Waste Reduction**: Unreleased print jobs are automatically purged after 24 hours.
- **Mobility**: Send a print job in Building A and release it in Building B or an international branch office.

---

## Step-by-Step Printer Mapping

### Method 1: Automated FollowMe Print Queue Mapping (Recommended)
1. Ensure your laptop is connected to the corporate office Wi-Fi (`Company-Corp`) or connected via Cisco AnyConnect VPN.
2. Press the Windows Key + R (`Win + R`) to open the **Run** dialog box.
3. Type the network print server path:
   `\\printserver01.company.local\FollowMe-Print`
4. Click **OK**. Windows will automatically connect, authenticate your credentials, and download the certified Type 4 universal print driver.
5. Once complete, `FollowMe-Print on printserver01` will appear in your Windows Printer Settings as the default printer.

### Method 2: Adding via Company Portal
1. Open the Windows Start menu and launch **Company Portal**.
2. Navigate to **Peripherals & Drivers**.
3. Locate **Corporate Secure Print FollowMe Queue** and click **Install**.

---

## Badge Registration at Physical Printer
The first time you tap your RFID employee badge at any Canon / Xerox multi-function printer:
1. Tap your badge against the contactless card reader (marked with the card logo).
2. The touchscreen terminal will display: *"Unrecognized Card. Please register your account."*
3. Type your corporate username (`firstname.lastname`) and your Active Directory password using the on-screen keyboard.
4. Press **Register**. Your badge is now permanently bound to your user account across all global offices. Subsequent print releases require only a single badge tap.

---

## Troubleshooting Print Spooler & Driver Issues

### 1. Clearing a Stuck Print Job (Windows Print Spooler Reset)
If a large document causes the print queue to freeze or hang:
1. Open Windows Search, type `cmd`, right-click **Command Prompt**, and select **Run as administrator**.
2. Stop the local print spooler service by typing:
   `net stop spooler`
3. Delete all cached spool files from the temporary printer directory:
   `del /Q /F /S "%systemroot%\System32\Spool\Printers\*.*"`
4. Restart the print spooler service:
   `net start spooler`
5. Resend your document to `FollowMe-Print`.

### 2. Error 0x0000011b (RPC Connection Failure)
- **Cause**: Windows security updates enforcing Point and Print RPC authentication privacy.
- **Resolution**:
  1. Open Registry Editor (`regedit`) as Administrator.
  2. Navigate to: `HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Print`
  3. Create a new `DWORD (32-bit)` value named `RpcAuthnLevelPrivacyEnabled` and set its value to `0`.
  4. Restart your computer.

### 3. Physical Printer Hardware Errors (Paper Jams & Out of Toner)
- **Paper Jam**: Follow the interactive visual guide displayed on the printer touchscreen. Most jams occur in **Tray 2** or the **Side Duplexer Cover (Door A)**. Gently pull the jammed paper in the direction of the paper path to avoid tearing.
- **Toner Replacement**: Replacement toner cartridges (Cyan, Magenta, Yellow, Black) and reams of 20lb white paper are stored in the IT Copy Supply Closet on each floor (door code: `2026`).
- **Hardware Fault / Broken Unit**: If the printer displays an error code (e.g., `E000-0010` Fuser Error), place the "Maintenance In Progress" sign on the device and submit an incident ticket with the printer asset tag (e.g., `PRN-BLD2-FL3`).
