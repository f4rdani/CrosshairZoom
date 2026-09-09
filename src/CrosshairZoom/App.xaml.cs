using System;
using System.Windows;
using System.Windows.Interop;
using CrosshairZoom.Interop;
using CrosshairZoom.Models;
using CrosshairZoom.Services;
using CrosshairZoom.Windows;

namespace CrosshairZoom
{
    public partial class App : Application
    {
        private OverlayWindow? _overlayWindow;
        private SettingsWindow? _settingsWindow;
        private Window? _messageWindow;

        private HotkeyService? _hotkeyService;
        private OverlayService? _overlayService;
        private SettingsService? _settingsService;
        private TrayIconService? _trayIconService;
        private MagnifierService? _magnifierService;

        private static readonly string LogPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_debug.log");

        private static void Log(string msg)
        {
            try
            {
                System.IO.File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\r\n");
            }
            catch { }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Log("OnStartup started");
            base.OnStartup(e);

            DispatcherUnhandledException += (s, ex) =>
            {
                Log($"Unhandled Exception: {ex.Exception}");
                MessageBox.Show(
                    $"Error: {ex.Exception.Message}\n\n{ex.Exception.StackTrace}",
                    "CrosshairZoom Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ex.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                Log($"AppDomain Unhandled: {ex.ExceptionObject}");
            };

            try
            {
                Log("Loading settings...");
                _settingsService = SettingsService.CurrentInstance;
                _settingsService.SettingsChanged += OnSettingsChanged;
                Log("Settings loaded");

                Log("Creating OverlayWindow...");
                _overlayWindow = new OverlayWindow();
                _overlayService = new OverlayService();
                _overlayService.SetOverlayWindow(_overlayWindow);
                Log("OverlayWindow created");

                // Initialize native 2x Magnifier Service
                Log("Creating MagnifierService...");
                _magnifierService = new MagnifierService();
                _magnifierService.Initialize();
                Log("MagnifierService created");

                Log("Creating message window...");
                _messageWindow = new Window
                {
                    Width = 1,
                    Height = 1,
                    WindowStyle = WindowStyle.None,
                    ShowInTaskbar = false,
                    ShowActivated = false,
                    Left = -100,
                    Top = -100
                };
                _messageWindow.Show();
                _messageWindow.Hide();
                Log("Message window created");

                var hwnd = new WindowInteropHelper(_messageWindow).Handle;

                // Setup global low-level keyboard hotkeys
                Log("Registering hotkeys...");
                _hotkeyService = new HotkeyService();
                _hotkeyService.RegisterAll(_settingsService.Current);
                _hotkeyService.HotkeyPressed += OnHotkeyPressed;

                // Setup native Win32 TrayIconService
                Log("Initializing TrayIconService...");
                _trayIconService = new TrayIconService();
                _trayIconService.Initialize(hwnd);
                _trayIconService.TrayIconClicked += ShowSettings;
                _trayIconService.TrayIconRightClicked += ShowSettings;
                Log("TrayIconService initialized");

                // Add WndProc hook for tray icon
                var source = HwndSource.FromHwnd(hwnd);
                source?.AddHook(_trayIconService.WndProc);
                Log("WndProc hooks added");

                // Setup Settings / Control Center Window
                _settingsWindow = new SettingsWindow();
                _settingsWindow.ToggleCrosshairRequested += ToggleCrosshair;
                _settingsWindow.ToggleZoomRequested += ToggleZoom;
                _settingsWindow.ExitAppRequested += ExitApplication;

                // Apply initial settings
                Log("Applying settings...");
                ApplySettings();
                Log("Settings applied");

                // Show overlay if enabled
                if (_settingsService.Current.CrosshairEnabled)
                {
                    Log("Showing overlay...");
                    _overlayService.Show();
                    Log("Overlay shown");
                }

                if (_overlayWindow != null)
                {
                    var overlayHwnd = new WindowInteropHelper(_overlayWindow).Handle;
                    _magnifierService.SetOverlayHwnd(overlayHwnd);
                }

                // Show Control Center Dashboard window on launch
                _settingsWindow.Show();
                Log("OnStartup finished successfully!");
            }
            catch (Exception ex)
            {
                Log($"Startup Exception: {ex}");
                MessageBox.Show(
                    $"Startup Error:\n{ex.Message}\n\n{ex.StackTrace}",
                    "CrosshairZoom", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void OnSettingsChanged()
        {
            ApplySettings();
        }

        private void OnHotkeyPressed(string action)
        {
            Log($"HotkeyPressed action: {action}");
            switch (action)
            {
                case "ToggleCrosshair":
                    ToggleCrosshair();
                    break;
                case "ToggleZoom":
                    ToggleZoom();
                    break;
                case "CycleZoomLevel":
                    ToggleZoom();
                    break;
                case "NextCrosshair":
                    CyclePreset();
                    break;
                case "ExitApp":
                    ExitApplication();
                    break;
            }
        }

        private void ToggleCrosshair()
        {
            _overlayService?.Toggle();
            if (_settingsService != null)
            {
                _settingsService.Current.CrosshairEnabled = _overlayService?.IsVisible ?? false;
            }
        }

        private void ToggleZoom()
        {
            if (_magnifierService == null) return;

            _magnifierService.Toggle();

            if (_magnifierService.IsVisible && _overlayWindow != null)
            {
                var overlayHwnd = new WindowInteropHelper(_overlayWindow).Handle;
                WindowHelper.SetTopmost(overlayHwnd);
            }
        }

        private void CyclePreset()
        {
            if (_settingsService == null) return;

            var shapes = new[] { CrosshairShape.Dot, CrosshairShape.Cross, CrosshairShape.CircleDot, CrosshairShape.Crosshair };
            int idx = _settingsService.Current.SelectedCrosshairPreset;
            idx = (idx + 1) % shapes.Length;
            _settingsService.Current.SelectedCrosshairPreset = idx;
            _settingsService.Current.Crosshair.Shape = shapes[idx];

            _overlayService?.UpdateCrosshair(_settingsService.Current.Crosshair);
            _settingsWindow?.LoadSettingsToUI();
        }

        private void ApplySettings()
        {
            if (_settingsService == null) return;

            _overlayService?.UpdateCrosshair(_settingsService.Current.Crosshair);

            if (_magnifierService != null)
            {
                _magnifierService.UpdateProfile(_settingsService.Current.Zoom, _settingsService.Current.Crosshair);
            }

            // Dynamically reload hotkeys in HotkeyService
            _hotkeyService?.RegisterAll(_settingsService.Current);

            // Dynamically update tray icon tooltip
            if (_trayIconService != null && _settingsService.Current.Hotkeys != null)
            {
                var hk = _settingsService.Current.Hotkeys;
                string chKey = hk.TryGetValue("ToggleCrosshair", out var b1) ? b1.ToDisplayString() : "F1";
                string zmKey = hk.TryGetValue("ToggleZoom", out var b2) ? b2.ToDisplayString() : "F2";
                string prKey = hk.TryGetValue("NextCrosshair", out var b3) ? b3.ToDisplayString() : "F4";
                _trayIconService.UpdateTooltip($"CrosshairZoom ({chKey}: Crosshair, {zmKey}: Zoom 2x, {prKey}: Preset)");
            }
        }

        private void ShowSettings()
        {
            if (_settingsWindow == null)
            {
                _settingsWindow = new SettingsWindow();
                _settingsWindow.ToggleCrosshairRequested += ToggleCrosshair;
                _settingsWindow.ToggleZoomRequested += ToggleZoom;
                _settingsWindow.ExitAppRequested += ExitApplication;
            }

            _settingsWindow.Show();
            _settingsWindow.WindowState = WindowState.Normal;
            _settingsWindow.Activate();
        }

        private void ExitApplication()
        {
            Log("ExitApplication called");
            _hotkeyService?.Dispose();
            _trayIconService?.Dispose();
            _magnifierService?.Dispose();

            if (_settingsWindow != null)
            {
                _settingsWindow.IsAppExiting = true;
                _settingsWindow.Close();
            }

            _overlayWindow?.Close();
            _messageWindow?.Close();

            Log("Calling Shutdown()");
            Shutdown();
            Environment.Exit(0);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log($"OnExit called with exit code: {e.ApplicationExitCode}");
            _hotkeyService?.Dispose();
            _trayIconService?.Dispose();
            _magnifierService?.Dispose();
            base.OnExit(e);
        }
    }
}
