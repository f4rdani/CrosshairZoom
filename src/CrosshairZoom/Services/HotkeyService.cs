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

        private readonly Dictionary<string, HotkeyBinding> _bindings = new();

        public HotkeyService()
        {
            _proc = HookCallback;
            _hookId = SetHook(_proc);
        }

        public void RegisterAll(AppSettings settings)
        {
            lock (_bindings)
            {
                _bindings.Clear();
                if (settings.Hotkeys != null)
                {
                    foreach (var (action, binding) in settings.Hotkeys)
                    {
                        if (binding != null && binding.Key > 0)
                        {
                            _bindings[action] = new HotkeyBinding(binding.ModifierKeys, binding.Key);
                        }
                    }
                }
            }
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

                // Ignore pure modifier presses
                if (vkCode != NativeMethods.VK_CONTROL &&
                    vkCode != NativeMethods.VK_SHIFT &&
                    vkCode != NativeMethods.VK_MENU &&
                    vkCode != NativeMethods.VK_LWIN &&
                    vkCode != NativeMethods.VK_RWIN)
                {
                    int currentModifiers = 0;
                    if ((NativeMethods.GetAsyncKeyState(NativeMethods.VK_MENU) & 0x8000) != 0) currentModifiers |= 1;    // Alt
                    if ((NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0) currentModifiers |= 2; // Ctrl
                    if ((NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0) currentModifiers |= 4;   // Shift
                    if ((NativeMethods.GetAsyncKeyState(NativeMethods.VK_LWIN) & 0x8000) != 0 || 
                        (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RWIN) & 0x8000) != 0) currentModifiers |= 8;   // Win

                    string? action = null;
                    lock (_bindings)
                    {
                        foreach (var (actionName, binding) in _bindings)
                        {
                            if (binding.Key == vkCode && binding.ModifierKeys == currentModifiers)
                            {
                                action = actionName;
                                break;
                            }
                        }
                    }

                    if (action != null)
                    {
                        Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            HotkeyPressed?.Invoke(action);
                        }));
                    }
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
