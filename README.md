# 🎯 CrosshairZoom

> **A modern, lightweight, GPU-accelerated gaming crosshair overlay & 2x digital screen magnifier for Windows.**

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0--windows-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D6?logo=windows)](https://microsoft.com/windows)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## ✨ Features

- 🎯 **Custom Crosshair Engine**:
  - Vector presets: **Dot (Precision Point)**, **Cross (+)**, **CircleDot**, and **Crosshair**.
  - **Custom PNG Image Support**: Load your own transparent `.png`, `.jpg`, or `.ico` reticles from disk.
  - Granular controls for Size, Line Thickness, Center Gap, Opacity, and High-Contrast Black Outlines.

- 🔍 **2x Hardware-Accelerated Digital Magnifier**:
  - Powered directly by the **Win32 Magnification API (`Magnification.dll`)** through the Desktop Window Manager (GPU).
  - Zero lag, zero screen tearing, and zero recursive feedback loop bugs.
  - Lens shapes: **Widescreen Tactical (650x450)**, **Large Square (550x550)**, and **Circular (550px)**.

- ↔️ **Precision X/Y Coordinate Shifting**:
  - Shift your crosshair and zoom lens horizontally (X) and vertically (Y) by -300px to +300px to perfectly align with off-center ADS camera perspectives (e.g. third-person shooters or custom gun sights).
  - Quick **⟲ Reset Center (0,0)** button.

- ⚡ **Realtime Instant Auto-Save**:
  - Every slider tweak and dropdown selection is immediately saved to `%AppData%/CrosshairZoom/settings.json` and rendered live in-game without manual save buttons.

- ⌨️ **Global Low-Level Hotkeys**:
  - Non-intrusive keyboard hooks (`WH_KEYBOARD_LL`) that work across borderless and windowed games without stealing window focus.

---

## ⌨️ Default Hotkeys

| Hotkey | Action |
|---|---|
| **`F1`** | Toggle Crosshair Overlay (Show / Hide) |
| **`F2`** | Toggle 2x Digital Screen Magnifier (Show / Hide) |
| **`F4`** | Cycle Next Crosshair Style Preset |
| **`Ctrl + Shift + Q`** | Exit & Close Application Completely |

---

## 🛠️ Requirements & Installation

### Requirements
- **OS**: Windows 10 / Windows 11 (64-bit)
- **Runtime**: [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Building from Source
```bash
# Clone the repository
git clone https://github.com/YOUR_USERNAME/CrosshairZoom.git

# Navigate to project directory
cd CrosshairZoom

# Build Release binary
dotnet build CrosshairZoom.sln -c Release

# Or publish self-contained / single-file release
dotnet publish src/CrosshairZoom/CrosshairZoom.csproj -c Release -r win-x64 --self-contained false -o publish
```

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.
