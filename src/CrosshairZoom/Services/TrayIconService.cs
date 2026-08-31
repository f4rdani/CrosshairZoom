using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using CrosshairZoom.Interop;

namespace CrosshairZoom.Services
{
    public class TrayIconService : IDisposable
    {
        private IntPtr _hwnd;
        private NativeMethods.NOTIFYICONDATA _nid;
        private IntPtr _hIcon = IntPtr.Zero;
        private bool _isAdded = false;

        public event Action? TrayIconClicked;
        public event Action? TrayIconRightClicked;

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr CreateIconIndirect(ref ICONINFO iconInfo);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint nPlanes, uint nBitCount, IntPtr lpBits);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern uint ExtractIconEx(string szFileName, int nIconIndex, out IntPtr phiconLarge, out IntPtr phiconSmall, uint nIcons);

        public void Initialize(IntPtr hwnd)
        {
            _hwnd = hwnd;
            _hIcon = LoadUnifiedIconHandle();

            _nid = new NativeMethods.NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NativeMethods.NOTIFYICONDATA>(),
                hWnd = _hwnd,
                uID = 1001,
                uFlags = NativeMethods.NIF_MESSAGE | NativeMethods.NIF_ICON | NativeMethods.NIF_TIP,
                uCallbackMessage = NativeMethods.WM_TRAYICON,
                hIcon = _hIcon,
                szTip = "CrosshairZoom (F1: Crosshair, F2: Zoom 2x, F4: Preset)"
            };

            _isAdded = NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_ADD, ref _nid);
        }

        private static IntPtr LoadUnifiedIconHandle()
        {
            try
            {
                using var process = Process.GetCurrentProcess();
                string? exePath = process.MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
                {
                    uint count = ExtractIconEx(exePath, 0, out IntPtr hLarge, out IntPtr hSmall, 1);
                    if (count > 0 && hSmall != IntPtr.Zero)
                    {
                        if (hLarge != IntPtr.Zero) DestroyIcon(hLarge);
                        return hSmall;
                    }
                    if (count > 0 && hLarge != IntPtr.Zero)
                    {
                        return hLarge;
                    }
                }
            }
            catch { }

            return CreateCrosshairIconHandle();
        }

        private static IntPtr CreateCrosshairIconHandle()
        {
            const int width = 32;
            const int height = 32;
            uint[] pixels = new uint[width * height];

            int cx = 16;
            int cy = 16;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double dx = x - cx + 0.5;
                    double dy = y - cy + 0.5;
                    double dist = Math.Sqrt(dx * dx + dy * dy);

                    // Dark circle background
                    if (dist <= 14)
                    {
                        pixels[y * width + x] = 0xEE0E1016; // Dark Obsidian
                    }

                    // Green outer ring
                    if (dist >= 11.5 && dist <= 13.5)
                    {
                        pixels[y * width + x] = 0xFF00FF88; // Neon Green
                    }

                    // Tactical ticks (Cyan)
                    if (((x == cx && (y <= 4 || y >= 28)) || (y == cy && (x <= 4 || x >= 28))) && dist <= 14)
                    {
                        pixels[y * width + x] = 0xFF00E5FF;
                    }

                    // Crosshair cross lines
                    if ((Math.Abs(x - cx) <= 0.8 && dist <= 11) || (Math.Abs(y - cy) <= 0.8 && dist <= 11))
                    {
                        pixels[y * width + x] = 0xFF00FF88;
                    }

                    // Center dot
                    if (dist <= 2.2)
                    {
                        pixels[y * width + x] = 0xFFFF2D41; // Bright Red Dot
                    }
                }
            }

            GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            IntPtr hbmColor = IntPtr.Zero;
            IntPtr hbmMask = IntPtr.Zero;
            IntPtr hIcon = IntPtr.Zero;

            try
            {
                hbmColor = CreateBitmap(width, height, 1, 32, handle.AddrOfPinnedObject());
                byte[] maskBytes = new byte[width * height / 8];
                GCHandle maskHandle = GCHandle.Alloc(maskBytes, GCHandleType.Pinned);
                try
                {
                    hbmMask = CreateBitmap(width, height, 1, 1, maskHandle.AddrOfPinnedObject());
                }
                finally
                {
                    maskHandle.Free();
                }

                var iconInfo = new ICONINFO
                {
                    fIcon = true,
                    xHotspot = 0,
                    yHotspot = 0,
                    hbmMask = hbmMask,
                    hbmColor = hbmColor
                };

                hIcon = CreateIconIndirect(ref iconInfo);
            }
            finally
            {
                handle.Free();
                if (hbmColor != IntPtr.Zero) DeleteObject(hbmColor);
                if (hbmMask != IntPtr.Zero) DeleteObject(hbmMask);
            }

            return hIcon;
        }

        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_TRAYICON)
            {
                int eventMsg = lParam.ToInt32();
                if (eventMsg == NativeMethods.WM_LBUTTONUP || eventMsg == NativeMethods.WM_LBUTTONDBLCLK)
                {
                    TrayIconClicked?.Invoke();
                    handled = true;
                }
                else if (eventMsg == NativeMethods.WM_RBUTTONUP)
                {
                    TrayIconRightClicked?.Invoke();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_isAdded)
            {
                NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_DELETE, ref _nid);
                _isAdded = false;
            }

            if (_hIcon != IntPtr.Zero)
            {
                DestroyIcon(_hIcon);
                _hIcon = IntPtr.Zero;
            }
        }
    }
}
