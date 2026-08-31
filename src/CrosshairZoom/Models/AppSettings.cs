using System.Collections.Generic;

namespace CrosshairZoom.Models;

public class HotkeyBinding
{
    public int ModifierKeys { get; set; }  // MOD_ALT, MOD_CTRL, MOD_SHIFT
    public int Key { get; set; }           // Virtual key code
}

public class AppSettings
{
    public CrosshairProfile Crosshair { get; set; } = new();
    public ZoomProfile Zoom { get; set; } = new();
    
    public Dictionary<string, HotkeyBinding> Hotkeys { get; set; } = new()
    {
        ["ToggleCrosshair"] = new() { ModifierKeys = 0, Key = 0x70 },  // F1
        ["ToggleZoom"] = new() { ModifierKeys = 0, Key = 0x71 },       // F2
        ["CycleZoomLevel"] = new() { ModifierKeys = 0, Key = 0x72 },   // F3
        ["NextCrosshair"] = new() { ModifierKeys = 0, Key = 0x73 },    // F4
        ["ExitApp"] = new() { ModifierKeys = 0x03, Key = 0x51 },       // Ctrl+Shift+Q
    };
    
    public bool StartWithWindows { get; set; } = false;
    public bool CrosshairEnabled { get; set; } = true;
    public bool ZoomEnabled { get; set; } = false;
    public int SelectedCrosshairPreset { get; set; } = 0;
}
