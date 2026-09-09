using System;
using System.Collections.Generic;
using System.Windows.Input;
using InputKey = System.Windows.Input.Key;

namespace CrosshairZoom.Models;

public class HotkeyBinding
{
    public int ModifierKeys { get; set; }  // 1=Alt, 2=Ctrl, 4=Shift, 8=Win
    public int Key { get; set; }           // Virtual key code

    public HotkeyBinding() { }

    public HotkeyBinding(int modifierKeys, int key)
    {
        ModifierKeys = modifierKeys;
        Key = key;
    }

    public string ToDisplayString()
    {
        if (Key == 0) return "None";

        var parts = new List<string>();
        if ((ModifierKeys & 2) != 0) parts.Add("Ctrl");
        if ((ModifierKeys & 1) != 0) parts.Add("Alt");
        if ((ModifierKeys & 4) != 0) parts.Add("Shift");
        if ((ModifierKeys & 8) != 0) parts.Add("Win");

        string keyName;
        try
        {
            var wpfKey = KeyInterop.KeyFromVirtualKey(Key);
            keyName = wpfKey switch
            {
                InputKey.None => Key switch
                {
                    0x70 => "F1", 0x71 => "F2", 0x72 => "F3", 0x73 => "F4",
                    0x74 => "F5", 0x75 => "F6", 0x76 => "F7", 0x77 => "F8",
                    0x78 => "F9", 0x79 => "F10", 0x7A => "F11", 0x7B => "F12",
                    _ => $"VK_0x{Key:X2}"
                },
                InputKey.D0 => "0", InputKey.D1 => "1", InputKey.D2 => "2", InputKey.D3 => "3", InputKey.D4 => "4",
                InputKey.D5 => "5", InputKey.D6 => "6", InputKey.D7 => "7", InputKey.D8 => "8", InputKey.D9 => "9",
                InputKey.NumPad0 => "Num 0", InputKey.NumPad1 => "Num 1", InputKey.NumPad2 => "Num 2",
                InputKey.NumPad3 => "Num 3", InputKey.NumPad4 => "Num 4", InputKey.NumPad5 => "Num 5",
                InputKey.NumPad6 => "Num 6", InputKey.NumPad7 => "Num 7", InputKey.NumPad8 => "Num 8", InputKey.NumPad9 => "Num 9",
                InputKey.Capital => "Caps Lock",
                InputKey.OemTilde => "~ (Tilde)",
                InputKey.OemPlus => "+",
                InputKey.OemMinus => "-",
                InputKey.Space => "Space",
                InputKey.Tab => "Tab",
                InputKey.Back => "Backspace",
                InputKey.Return => "Enter",
                InputKey.Escape => "Esc",
                InputKey.Insert => "Insert",
                InputKey.Delete => "Delete",
                InputKey.Home => "Home",
                InputKey.End => "End",
                InputKey.PageUp => "Page Up",
                InputKey.PageDown => "Page Down",
                _ => wpfKey.ToString()
            };
        }
        catch
        {
            keyName = $"Key({Key})";
        }

        parts.Add(keyName);
        return string.Join(" + ", parts);
    }
}

public class AppSettings
{
    public CrosshairProfile Crosshair { get; set; } = new();
    public ZoomProfile Zoom { get; set; } = new();
    
    public Dictionary<string, HotkeyBinding> Hotkeys { get; set; } = GetDefaultHotkeys();
    
    public bool StartWithWindows { get; set; } = false;
    public bool CrosshairEnabled { get; set; } = true;
    public bool ZoomEnabled { get; set; } = false;
    public int SelectedCrosshairPreset { get; set; } = 0;

    public static Dictionary<string, HotkeyBinding> GetDefaultHotkeys() => new()
    {
        ["ToggleCrosshair"] = new(0, 0x70),              // F1
        ["ToggleZoom"] = new(0, 0x71),                   // F2
        ["NextCrosshair"] = new(0, 0x73),                // F4
        ["ExitApp"] = new(2 | 4, 0x51),                  // Ctrl + Shift + Q
    };

    public void EnsureDefaultHotkeys()
    {
        Hotkeys ??= new Dictionary<string, HotkeyBinding>();
        var defaults = GetDefaultHotkeys();
        foreach (var kvp in defaults)
        {
            if (!Hotkeys.ContainsKey(kvp.Key) || Hotkeys[kvp.Key] == null)
            {
                Hotkeys[kvp.Key] = kvp.Value;
            }
        }
    }
}
