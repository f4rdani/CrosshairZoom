# 🎯 CrosshairZoom

> **A modern, lightweight, GPU-accelerated gaming crosshair overlay & 2x digital screen magnifier for Windows.**

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0--windows-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D6?logo=windows)](https://microsoft.com/windows)
[![Build](https://github.com/f4rdani/CrosshairZoom/actions/workflows/build.yml/badge.svg)](https://github.com/f4rdani/CrosshairZoom/actions)
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

- ⌨️ **Fully Customizable Global Hotkeys**:
  - Rebind and customize any action directly from the **⌨️ Hotkeys** tab in the Control Center (e.g., set F2 for Crosshair, or any desired key).
  - Supports single keys (`F1`–`F12`, alphanumeric, `~`, `Space`, etc.) and modifier combinations (`Ctrl`, `Alt`, `Shift`, `Win`).
  - Automatic duplicate key conflict resolution and instant **⟲ Reset All Defaults** option.
  - Non-intrusive low-level keyboard hooks (`WH_KEYBOARD_LL`) that work across borderless and windowed games without stealing window focus.

- ⚡ **Realtime Instant Auto-Save**:
  - Every slider tweak, hotkey rebind, and dropdown selection is immediately saved to `%AppData%/CrosshairZoom/settings.json` and rendered live in-game without manual save buttons.

---

## ⌨️ Default Hotkeys

> **Note**: All hotkeys can be rebound and customized in real-time in the **Control Center → ⌨️ Hotkeys** tab.

| Action | Default Hotkey | Customizable |
|---|---|---|
| **Toggle Crosshair Overlay** | `F1` | ✅ Yes (e.g. F2, X, CapsLock) |
| **Toggle 2x Screen Magnifier Lens** | `F2` | ✅ Yes |
| **Cycle Crosshair Style Preset** | `F4` | ✅ Yes |
| **Exit CrosshairZoom Completely** | `Ctrl + Shift + Q` | ✅ Yes |

---

## 🛠️ Requirements & Installation

### Requirements
- **OS**: Windows 10 / Windows 11 (64-bit)
- **Runtime**: [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Building from Source
```bash
# Clone the repository
git clone https://github.com/f4rdani/CrosshairZoom.git

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
