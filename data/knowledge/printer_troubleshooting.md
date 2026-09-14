---
DocumentName: Network Printer Mapping & Diagnostics
Version: 1.0
Section: Section 4 - Peripherals & Hardware
Page: 1
Approved: true
Active: true
---
# Network Printer Mapping & Diagnostics

## Adding a Floor Printer
1. Open Windows Run prompt (`Win + R`).
2. Enter `\\printserver01\Printers`.
3. Double-click the desired printer (e.g. `PRN-FLOOR-03`).

## Print Spooler Troubleshooting
1. Open Services (`services.msc`).
2. Locate `Print Spooler`, right-click and click `Restart`.
3. If paper jam occurs, clear physical tray and power cycle printer.
