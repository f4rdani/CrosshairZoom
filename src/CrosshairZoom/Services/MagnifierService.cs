using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using CrosshairZoom.Interop;
using CrosshairZoom.Models;

namespace CrosshairZoom.Services
{
    public class MagnifierService : IDisposable
    {
        private IntPtr _hwndHost = IntPtr.Zero;
        private IntPtr _hwndMag = IntPtr.Zero;
        private IntPtr _overlayHwnd = IntPtr.Zero;
        private DispatcherTimer? _timer;
        private bool _isInitialized = false;
        private bool _isVisible = false;
        
        private int _lensWidth = 650;
        private int _lensHeight = 450;
        private int _cornerRadius = 16;
        private int _offsetX = 0;
        private int _offsetY = 0;
        private MagnifierShape _shape = MagnifierShape.WideRectangle;
        private bool _disposed = false;

        public bool IsVisible => _isVisible;
        public int LensWidth => _lensWidth;
        public int LensHeight => _lensHeight;
        public int OffsetX => _offsetX;
        public int OffsetY => _offsetY;
        public MagnifierShape Shape => _shape;

        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = NativeMethods.MagInitialize();
            if (!_isInitialized) return;

            CreateHostAndMagWindow();

            _timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(1000.0 / 60.0) // 60 FPS
            };
            _timer.Tick += (s, e) => UpdateSource();
        }

        private void CreateHostAndMagWindow()
        {
            if (!_isInitialized) return;

            IntPtr hInstance = NativeMethods.GetModuleHandle(null);

            int screenW = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
            int screenH = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);

            int left = (screenW - _lensWidth) / 2 + _offsetX;
            int top = (screenH - _lensHeight) / 2 + _offsetY;

            int exStyle = NativeMethods.WS_EX_TOPMOST | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;
            uint style = 0x80000000; // WS_POPUP

            _hwndHost = NativeMethods.CreateWindowEx(
                exStyle,
                "Static",
                "CrosshairZoomLensHost",
                style,
                left, top, _lensWidth, _lensHeight,
                IntPtr.Zero,
                IntPtr.Zero,
                hInstance,
                IntPtr.Zero
            );

            if (_hwndHost == IntPtr.Zero) return;

            WindowHelper.MakeClickThrough(_hwndHost);

            _hwndMag = NativeMethods.CreateWindowEx(
                0,
                NativeMethods.WC_MAGNIFIER,
                "CrosshairZoomLensMag",
                (uint)(NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE),
                0, 0, _lensWidth, _lensHeight,
                _hwndHost,
                IntPtr.Zero,
                hInstance,
                IntPtr.Zero
            );

            if (_hwndMag != IntPtr.Zero)
            {
                var transform = new NativeMethods.MAGTRANSFORM
                {
                    v00 = 2.0f, v01 = 0.0f, v02 = 0.0f,
                    v10 = 0.0f, v11 = 2.0f, v12 = 0.0f,
                    v20 = 0.0f, v21 = 0.0f, v22 = 1.0f
                };
                NativeMethods.MagSetWindowTransform(_hwndMag, ref transform);

                UpdateFilter();
            }

            ApplyShape();
        }

        public void SetOverlayHwnd(IntPtr overlayHwnd)
        {
            _overlayHwnd = overlayHwnd;
            UpdateFilter();
        }

        private void UpdateFilter()
        {
            if (_hwndMag == IntPtr.Zero || _hwndHost == IntPtr.Zero) return;

            if (_overlayHwnd != IntPtr.Zero)
            {
                NativeMethods.MagSetWindowFilterList(_hwndMag, NativeMethods.MW_FILTERMODE_EXCLUDE, 2, new[] { _hwndHost, _overlayHwnd });
            }
            else
            {
                NativeMethods.MagSetWindowFilterList(_hwndMag, NativeMethods.MW_FILTERMODE_EXCLUDE, 1, new[] { _hwndHost });
            }
        }

        public void UpdateLens(int width, int height, MagnifierShape shape, int cornerRadius = 16, int offsetX = 0, int offsetY = 0)
        {
            _lensWidth = Math.Max(200, width);
            _lensHeight = Math.Max(150, height);
            _shape = shape;
            _cornerRadius = cornerRadius;
            _offsetX = offsetX;
            _offsetY = offsetY;

            if (_hwndHost == IntPtr.Zero) return;

            int screenW = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
            int screenH = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
            int left = (screenW - _lensWidth) / 2 + _offsetX;
            int top = (screenH - _lensHeight) / 2 + _offsetY;

            NativeMethods.SetWindowPos(_hwndHost, NativeMethods.HWND_TOPMOST, left, top, _lensWidth, _lensHeight, NativeMethods.SWP_NOACTIVATE);

            if (_hwndMag != IntPtr.Zero)
            {
                NativeMethods.SetWindowPos(_hwndMag, IntPtr.Zero, 0, 0, _lensWidth, _lensHeight, NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOMOVE);
            }

            ApplyShape();
            UpdateSource();
        }

        public void UpdateProfile(ZoomProfile profile, CrosshairProfile? crosshair = null)
        {
            int w = profile.Width > 0 ? profile.Width : 650;
            int h = profile.Height > 0 ? profile.Height : 450;
            int ox = profile.SyncWithCrosshair && crosshair != null ? (int)crosshair.OffsetX : profile.OffsetX;
            int oy = profile.SyncWithCrosshair && crosshair != null ? (int)crosshair.OffsetY : profile.OffsetY;
            
            if (profile.Shape == MagnifierShape.Circular || profile.IsCircular)
            {
                int s = profile.LensSize > 0 ? profile.LensSize : 550;
                w = s;
                h = s;
                UpdateLens(w, h, MagnifierShape.Circular, 0, ox, oy);
            }
            else if (profile.Shape == MagnifierShape.LargeSquare)
            {
                int s = Math.Max(w, h);
                UpdateLens(s, s, MagnifierShape.LargeSquare, profile.CornerRadius, ox, oy);
            }
            else
            {
                UpdateLens(w, h, MagnifierShape.WideRectangle, profile.CornerRadius, ox, oy);
            }
        }

        private void ApplyShape()
        {
            if (_hwndHost == IntPtr.Zero) return;

            IntPtr hRgn;
            if (_shape == MagnifierShape.Circular)
            {
                hRgn = NativeMethods.CreateEllipticRgn(0, 0, _lensWidth, _lensHeight);
            }
            else if (_cornerRadius > 0)
            {
                hRgn = NativeMethods.CreateRoundRectRgn(0, 0, _lensWidth, _lensHeight, _cornerRadius * 2, _cornerRadius * 2);
            }
            else
            {
                hRgn = NativeMethods.CreateRectRgn(0, 0, _lensWidth, _lensHeight);
            }

            NativeMethods.SetWindowRgn(_hwndHost, hRgn, true);
        }

        public void Show()
        {
            if (!_isInitialized || _hwndHost == IntPtr.Zero)
            {
                Initialize();
            }

            if (_hwndHost != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(_hwndHost, 4); // SW_SHOWNOACTIVATE
                WindowHelper.SetTopmost(_hwndHost);
                _isVisible = true;
                UpdateSource();
                _timer?.Start();
            }
        }

        public void Hide()
        {
            if (_hwndHost != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(_hwndHost, 0); // SW_HIDE
                _isVisible = false;
                _timer?.Stop();
            }
        }

        public void Toggle()
        {
            if (_isVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        private void UpdateSource()
        {
            if (_hwndMag == IntPtr.Zero || !_isVisible) return;

            int screenW = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
            int screenH = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);

            int centerX = (screenW / 2) + _offsetX;
            int centerY = (screenH / 2) + _offsetY;

            const float zoom = 2.0f;
            int srcW = (int)(_lensWidth / zoom);
            int srcH = (int)(_lensHeight / zoom);

            var rect = new NativeMethods.RECT
            {
                left = centerX - (srcW / 2),
                top = centerY - (srcH / 2),
                right = centerX + (srcW / 2),
                bottom = centerY + (srcH / 2)
            };

            NativeMethods.MagSetWindowSource(_hwndMag, rect);
            NativeMethods.InvalidateRect(_hwndMag, IntPtr.Zero, false);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _timer?.Stop();
                Hide();

                if (_hwndHost != IntPtr.Zero)
                {
                    NativeMethods.DestroyWindow(_hwndHost);
                    _hwndHost = IntPtr.Zero;
                }

                if (_isInitialized)
                {
                    NativeMethods.MagUninitialize();
                    _isInitialized = false;
                }

                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}
