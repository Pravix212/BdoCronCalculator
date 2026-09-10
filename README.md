# Black Desert Cron Stone Calculator

A lightweight, always-on-top desktop calculator built for Black Desert Online players. It streamlines enhancement cost calculations, provides instant conversions for vendor and outfit-extracted Cron Stones, and dynamically formats large silver amounts into readable units (Millions, Billions, Trillions).

---

## Features

### Overlay & Always-On-Top
- Stays pinned above the game window during active gameplay (borderless or windowed mode).
- Toggleable pin control on the title bar.
- Minimal dark interface designed to match in-game UI aesthetics.

### Cron Stone Cost Estimation
- **Vendor Crons (`CRON 3M`)**: Calculates direct purchase cost from the NPC Blacksmith at 3,000,000 silver per stone.
- **Outfit Extraction (`Outfit 2.18M`)**: Calculates effective cost when extracting Cron Stones from Central Market Pearl Outfits (~2,180,000 silver per stone).

### Central Market Tax Calculator
- **Net Silver Calculation (`💰 Tax`)**: Instantly calculates post-tax silver earnings for sold items.
- **Configurable Tax Settings (`⚙`)**:
  - **Value Pack**: Toggle +30% silver collection bonus (base retention goes from 65% to 84.5%).
  - **Rich Merchant's Ring**: Toggle +5% market silver bonus.
  - **Family Fame**: Configurable Fame points with automatic tier bonus calculation (+0.0% to +1.5%).
  - Live readout of effective payout rate (e.g., 85.48%) and net tax percentage.

### Currency Readout
- Automatically converts large numbers into formatted currency text:
  - `4,050,000,000` $\rightarrow$ `4.05 Billion Silver (4,050 M)`
  - `850,000,000` $\rightarrow$ `850.00 Million Silver (850M)`
  - `1,250,000,000,000` $\rightarrow$ `1.25 Trillion Silver`

### Keypad & Multipliers
- Standard arithmetic operations: addition, subtraction, multiplication, division, negation, and clear functions.
- Quick multipliers (`+k`, `+M`, `+B`) for rapid input without manual zero entry.
- Clipboard copy button for sharing formatted numbers.

---

## Keyboard Shortcuts

| Key | Function |
|---|---|
| `0` - `9` / `NumPad 0` - `9` | Digit input |
| `.` / `Decimal` | Decimal separator |
| `+`, `-`, `*`, `/` | Basic arithmetic operators |
| `Enter` / `=` | Calculate result |
| `Backspace` | Remove last character |
| `Escape` / `Delete` | Clear all (`C`) or dismiss settings overlay |
| `C` | Execute Vendor Cron calculation (`3M`) |
| `T` | Execute Central Market Tax calculation |
| `K` | Multiply current value by 1,000 (`k`) |
| `M` | Multiply current value by 1,000,000 (`M`) |
| `B` | Multiply current value by 1,000,000,000 (`B`) |
| `Ctrl + C` | Copy formatted result to clipboard |

---

## Installation & Downloads

Pre-compiled standalone packages for Windows and Android are available on the [Releases](../../releases/latest) page.

### Windows Desktop (.exe)
1. Download `BdoCronCalculator.exe` or `BdoCronCalculator-Windows.zip`.
2. Run `BdoCronCalculator.exe` directly (portable, no installer needed).
3. *(Optional)* Right-click `BdoCronCalculator.exe` and select **Pin to taskbar**.

### Android Mobile (.apk)
1. Download `BdoCronCalculator.apk` on your Android device.
2. Open the downloaded file and tap **Install** (if prompted, allow installs from your browser/file manager).
3. Open **BDO Cron Calculator** from your app drawer.

---

## Building from Source

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (Windows x64)

### Build
```powershell
dotnet build BdoCronCalculator.csproj -c Release
```

### Publish Self-Contained / Portable Binary
```powershell
dotnet publish BdoCronCalculator.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish/
```

### Run Tests
```powershell
dotnet test BdoCronCalculator.Tests/BdoCronCalculator.Tests.csproj
```

---

## Technical Details

- **Framework**: .NET 9 (WPF / C#)
- **Window Management**: Win32 `Topmost` overlay layering and non-client drag handling
- **Architecture**: Decoupled calculation engine with invariant culture formatting

---

## License

This project is licensed under the MIT License.
