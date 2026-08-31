using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using CrosshairZoom.Interop;
using CrosshairZoom.Models;

namespace CrosshairZoom.Services
{
    public class HotkeyService : IDisposable
    {
        private IntPtr _hookId = IntPtr.Zero;
        private NativeMethods.LowLevelKeyboardProc? _proc;
        private bool _disposed;

        public event Action<string>? HotkeyPressed;

        public HotkeyService()
        {
            _proc = HookCallback;
            _hookId = SetHook(_proc);
        }

        public void RegisterAll(AppSettings settings)
        {
            // Settings can be used for custom bindings if configured
        }

        private IntPtr SetHook(NativeMethods.LowLevelKeyboardProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            IntPtr moduleHandle = NativeMethods.GetModuleHandle(curModule?.ModuleName);
            return NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, proc, moduleHandle, 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)NativeMethods.WM_KEYDOWN || wParam == (IntPtr)NativeMethods.WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
                bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
                bool alt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;

                string? action = null;

                // F1: Toggle Crosshair (VK_F1 = 0x70 = 112)
                if (vkCode == 0x70 && !ctrl && !alt && !shift)
                {
                    action = "ToggleCrosshair";
                }
                // F2: Toggle Zoom (VK_F2 = 0x71 = 113)
                else if (vkCode == 0x71 && !ctrl && !alt && !shift)
                {
                    action = "ToggleZoom";
                }
                // F3: Cycle Zoom (VK_F3 = 0x72 = 114)
                else if (vkCode == 0x72 && !ctrl && !alt && !shift)
                {
                    action = "CycleZoomLevel";
                }
                // F4: Next Crosshair Preset (VK_F4 = 0x73 = 115)
                else if (vkCode == 0x73 && !ctrl && !alt && !shift)
                {
                    action = "NextCrosshair";
                }
                // Ctrl + Shift + Q: Exit
                else if (vkCode == 0x51 && ctrl && shift)
                {
                    action = "ExitApp";
                }

                if (action != null)
                {
                    Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        HotkeyPressed?.Invoke(action);
                    }));
                }
            }

            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_hookId != IntPtr.Zero)
                {
                    NativeMethods.UnhookWindowsHookEx(_hookId);
                    _hookId = IntPtr.Zero;
                }
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}
