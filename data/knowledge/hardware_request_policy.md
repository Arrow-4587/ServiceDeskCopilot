---
DocumentName: Laptop Hardware Procurement Policy
Version: 2.0
Section: Section 6 - Hardware Assets
Page: 1
Approved: true
Active: true
---
# Laptop Hardware Procurement, Lifecycle & Peripheral Policy

## Overview & Scope
This policy establishes standard equipment specifications, eligibility criteria, procurement workflows, and replacement protocols for all computer hardware, mobile workstations, monitors, and peripheral assets provided to corporate employees.

---

## Standard Workstation Provisioning Tiers
IT Asset Management maintains standardized hardware tiers configured with corporate security baselines, BitLocker encryption, and Intune endpoint management:

### 1. Standard Business Tier (General Corporate)
- **Target Roles**: Sales, Human Resources, Finance, Legal, Marketing, Operations, Customer Success.
- **Hardware Specs**: Dell Latitude 5440 or Lenovo ThinkPad T14s.
  - Processor: Intel Core i7-1365U / AMD Ryzen 7 PRO
  - Memory: 16 GB DDR5 RAM
  - Storage: 512 GB PCIe NVMe SSD
  - Display: 14" Full HD (1920x1080) Anti-Glare, Integrated Webcam with Privacy Shutter

### 2. Engineering & Developer Tier (High Performance)
- **Target Roles**: Software Engineers, Systems Architects, DevOps Engineers, Data Scientists, Security Engineers.
- **Hardware Specs (Windows/Linux)**: Dell Precision 5680 Mobile Workstation.
  - Processor: Intel Core i9-13900H / 14-Core
  - Memory: 32 GB or 64 GB DDR5 RAM
  - Storage: 1 TB or 2 TB PCIe Gen4 NVMe SSD
  - Dedicated GPU: NVIDIA RTX A2000 Ada (8GB GDDR6)
- **Hardware Specs (macOS Option)**: Apple MacBook Pro 16" (Space Black).
  - Chip: Apple M3 Pro / M3 Max chip
  - Unified Memory: 36 GB or 48 GB Unified Memory
  - Storage: 1 TB PCIe SSD

### 3. Executive Ultra-Portable Tier
- **Target Roles**: Vice Presidents, Directors, C-Suite Executives.
- **Hardware Specs**: Dell XPS 13 Plus / Lenovo ThinkPad X1 Carbon or Apple MacBook Air 15".
  - Ultra-lightweight form factor (< 2.7 lbs), 16 GB RAM, 512 GB SSD.

---

## Hardware Lifecycle & Standard Refresh Policy
- **Refresh Cycle**: Standard laptops are eligible for lifecycle refresh every **36 months (3 years)** from the original provisioning date.
- **Automated Eligibility Notifications**: The IT Asset Management System automatically checks asset ages and sends an email invitation to order a replacement laptop **60 days prior** to the 36-month maturity mark.
- **Early Upgrades**: Upgrades before 36 months require written business justification and Vice President cost-center budget approval.

---

## Damaged, Lost or Malfunctioning Hardware Replacement
For hardware that is physically damaged, inoperable, or exhibiting component failure:

### 1. On-Site Hot-Swap Service (Campus Offices)
- Employees near a corporate office campus can visit the **IT Walk-Up Service Bar** without an appointment.
- Technicians perform immediate diagnosis. If parts cannot be repaired within 15 minutes, an identical pre-imaged replacement laptop is provisioned immediately ("Hot-Swap"), with OneDrive and Enterprise State Roaming restoring user files within 30 minutes.

### 2. Remote Replacement & Overnight Courier Dispatch
- If working remotely, submit an Incident Draft through the IT Service Desk Copilot or portal.
- Select the hardware issue category (e.g., Cracked Screen, Liquid Spill, Battery Swelling, Motherboard Failure).
- An emergency replacement laptop is pre-imaged and dispatched via overnight courier (FedEx Priority / DHL Express) along with a pre-paid return shipping box for the damaged unit.

---

## Peripheral & Home Office Equipment Catalog
Employees can request standard desk peripherals via the IT Self-Service Portal under `Hardware Catalog`:
- **Monitors**: Dell UltraSharp 27" 4K USB-C Hub Monitor (U2723QE) with integrated 90W power delivery.
- **Docking Stations**: Dell Thunderbolt 4 Dock (WD22TB4) or CalDigit TS4 for Mac.
- **Input Devices**: Logitech MX Master 3S Wireless Mouse, Logitech MX Keys Wireless Keyboard, or Ergo Split Keyboard.
- **Webcams & Audio**: Poly Studio P5 Full HD Webcam, Jabra Evolve2 65 Noise-Cancelling Bluetooth Headset.
- **Approval Threshold**: Standard catalog items under $250 are automatically approved. Non-standard specialized hardware exceeding $250 requires direct Line Manager budget approval.

---

## Asset Decommissioning & Data Sanitization
All decommissioned and returned hardware is sanitized in strict accordance with the NIST SP 800-88 Rev. 1 / DoD 5220.22-M data destruction standard:
- SSDs undergo cryptographic erase and factory secure erase.
- Employees must return corporate equipment within 5 business days following receipt of replacement or upon company departure.
