using System;
using System.Windows;
using System.Windows.Interop;
using CrosshairZoom.Interop;
using CrosshairZoom.Models;

namespace CrosshairZoom.Windows
{
    public partial class OverlayWindow : Window
    {
        public OverlayWindow()
        {
            InitializeComponent();
            SourceInitialized += OnSourceInitialized;
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowHelper.MakeClickThrough(hwnd);
            HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_MOUSEACTIVATE = 0x0021;
            const int MA_NOACTIVATE = 3;

            if (msg == WM_MOUSEACTIVATE)
            {
                handled = true;
                return new IntPtr(MA_NOACTIVATE);
            }

            return IntPtr.Zero;
        }

        public void UpdateCrosshair(CrosshairProfile profile)
        {
            Crosshair.UpdateProfile(profile);
        }
    }
}
