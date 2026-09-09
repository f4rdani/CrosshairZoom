using System;
using System.Collections.Generic;
using System.Windows.Input;

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
                Key.None => Key switch
                {
                    0x70 => "F1", 0x71 => "F2", 0x72 => "F3", 0x73 => "F4",
                    0x74 => "F5", 0x75 => "F6", 0x76 => "F7", 0x77 => "F8",
                    0x78 => "F9", 0x79 => "F10", 0x7A => "F11", 0x7B => "F12",
                    _ => $"VK_0x{Key:X2}"
                },
                Key.D0 => "0", Key.D1 => "1", Key.D2 => "2", Key.D3 => "3", Key.D4 => "4",
                Key.D5 => "5", Key.D6 => "6", Key.D7 => "7", Key.D8 => "8", Key.D9 => "9",
                Key.NumPad0 => "Num 0", Key.NumPad1 => "Num 1", Key.NumPad2 => "Num 2",
                Key.NumPad3 => "Num 3", Key.NumPad4 => "Num 4", Key.NumPad5 => "Num 5",
                Key.NumPad6 => "Num 6", Key.NumPad7 => "Num 7", Key.NumPad8 => "Num 8", Key.NumPad9 => "Num 9",
                Key.Capital => "Caps Lock",
                Key.OemTilde => "~ (Tilde)",
                Key.OemPlus => "+",
                Key.OemMinus => "-",
                Key.Space => "Space",
                Key.Tab => "Tab",
                Key.Back => "Backspace",
                Key.Return => "Enter",
                Key.Escape => "Esc",
                Key.Insert => "Insert",
                Key.Delete => "Delete",
                Key.Home => "Home",
                Key.End => "End",
                Key.PageUp => "Page Up",
                Key.PageDown => "Page Down",
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
