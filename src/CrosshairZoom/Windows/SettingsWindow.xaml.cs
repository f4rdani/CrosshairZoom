using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using CrosshairZoom.Controls;
using CrosshairZoom.Models;
using CrosshairZoom.Services;

namespace CrosshairZoom.Windows
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _currentSettings = new();
        private bool _isInitializing = true;
        public bool IsAppExiting { get; set; } = false;

        private string? _recordingAction;
        private Button? _recordingButton;
        private Brush? _originalButtonBorder;
        private Brush? _originalButtonBackground;

        public event Action? ToggleCrosshairRequested;
        public event Action? ToggleZoomRequested;
        public event Action? ExitAppRequested;

        public SettingsWindow()
        {
            InitializeComponent();
            Loaded += SettingsWindow_Loaded;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _currentSettings = SettingsService.CurrentInstance.Current;
            LoadSettingsToUI();
            _isInitializing = false;
            UpdatePreviewAndAutoSave();
        }

        public void LoadSettingsToUI()
        {
            _isInitializing = true;

            if (_currentSettings.Crosshair != null)
            {
                int shapeIdx = (int)_currentSettings.Crosshair.Shape;
                if (ShapeCombo != null && ShapeCombo.Items.Count > shapeIdx)
                {
                    ShapeCombo.SelectedIndex = shapeIdx;
                }

                if (CustomPathText != null)
                {
                    CustomPathText.Text = string.IsNullOrWhiteSpace(_currentSettings.Crosshair.CustomImagePath) 
                        ? "No custom image selected" 
                        : Path.GetFileName(_currentSettings.Crosshair.CustomImagePath);
                    CustomPathText.ToolTip = _currentSettings.Crosshair.CustomImagePath;
                }

                if (ColorText != null) ColorText.Text = _currentSettings.Crosshair.Color;
                if (SizeSlider != null) SizeSlider.Value = _currentSettings.Crosshair.Size;
                if (ThicknessSlider != null) ThicknessSlider.Value = _currentSettings.Crosshair.Thickness;
                if (GapSlider != null) GapSlider.Value = _currentSettings.Crosshair.GapSize;
                if (OffsetXSlider != null) OffsetXSlider.Value = _currentSettings.Crosshair.OffsetX;
                if (OffsetYSlider != null) OffsetYSlider.Value = _currentSettings.Crosshair.OffsetY;
                if (OutlineCheck != null) OutlineCheck.IsChecked = _currentSettings.Crosshair.ShowOutline;

                if (SizeValueLabel != null && SizeSlider != null) SizeValueLabel.Text = $"{(int)SizeSlider.Value}px";
                if (ThicknessValueLabel != null && ThicknessSlider != null) ThicknessValueLabel.Text = $"{(int)ThicknessSlider.Value}px";
                if (GapValueLabel != null && GapSlider != null) GapValueLabel.Text = $"{(int)GapSlider.Value}px";
                if (OffsetXLabel != null && OffsetXSlider != null) OffsetXLabel.Text = $"{(int)OffsetXSlider.Value}px";
                if (OffsetYLabel != null && OffsetYSlider != null) OffsetYLabel.Text = $"{(int)OffsetYSlider.Value}px";
            }

            if (_currentSettings.Zoom != null)
            {
                int shapeIdx = 0;
                if (_currentSettings.Zoom.Shape == MagnifierShape.LargeSquare) shapeIdx = 1;
                else if (_currentSettings.Zoom.Shape == MagnifierShape.Circular || _currentSettings.Zoom.IsCircular) shapeIdx = 2;

                if (LensShapeCombo != null && LensShapeCombo.Items.Count > shapeIdx)
                {
                    LensShapeCombo.SelectedIndex = shapeIdx;
                }

                int w = _currentSettings.Zoom.Width > 0 ? _currentSettings.Zoom.Width : 650;
                int h = _currentSettings.Zoom.Height > 0 ? _currentSettings.Zoom.Height : 450;

                if (LensWidthSlider != null) LensWidthSlider.Value = w;
                if (LensHeightSlider != null) LensHeightSlider.Value = h;
                if (SyncPositionCheck != null) SyncPositionCheck.IsChecked = _currentSettings.Zoom.SyncWithCrosshair;

                if (LensWidthLabel != null) LensWidthLabel.Text = $"{w}px";
                if (LensHeightLabel != null) LensHeightLabel.Text = $"{h}px";
            }

            UpdateHotkeyUI();

            _isInitializing = false;
        }

        public void UpdateHotkeyUI()
        {
            _currentSettings.EnsureDefaultHotkeys();

            if (BtnHotkeyCrosshair != null && _currentSettings.Hotkeys.TryGetValue("ToggleCrosshair", out var chk))
            {
                string txt = chk.ToDisplayString();
                BtnHotkeyCrosshair.Content = txt;
                if (QuickCrosshairBtn != null) QuickCrosshairBtn.Content = $"✚ Crosshair ({txt})";
            }

            if (BtnHotkeyZoom != null && _currentSettings.Hotkeys.TryGetValue("ToggleZoom", out var zmk))
            {
                string txt = zmk.ToDisplayString();
                BtnHotkeyZoom.Content = txt;
                if (QuickZoomBtn != null) QuickZoomBtn.Content = $"🔍 Zoom 2x ({txt})";
                if (ZoomHintText != null) ZoomHintText.Text = $"💡 Press {txt} in-game anytime to toggle the 2x hardware-accelerated screen magnifier.";
            }

            if (BtnHotkeyPreset != null && _currentSettings.Hotkeys.TryGetValue("NextCrosshair", out var prk))
            {
                BtnHotkeyPreset.Content = prk.ToDisplayString();
            }

            if (BtnHotkeyExit != null && _currentSettings.Hotkeys.TryGetValue("ExitApp", out var exk))
            {
                BtnHotkeyExit.Content = exk.ToDisplayString();
            }
        }

        private void OnHotkeyButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string action) return;

            if (_recordingAction != null)
            {
                CancelHotkeyRecording();
            }

            _recordingAction = action;
            _recordingButton = btn;
            _originalButtonBorder = btn.BorderBrush;
            _originalButtonBackground = btn.Background;

            btn.BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x88));
            btn.Background = new SolidColorBrush(Color.FromRgb(0x22, 0x38, 0x2E));
            btn.Content = "Press key...";

            if (HotkeyStatusText != null)
            {
                HotkeyStatusText.Text = $"⏺ Listening: Press any key/combination for '{GetActionTitle(action)}'... (Esc to cancel)";
                HotkeyStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x88));
            }
        }

        private static string GetActionTitle(string action) => action switch
        {
            "ToggleCrosshair" => "Toggle Crosshair Overlay",
            "ToggleZoom" => "Toggle Digital Zoom",
            "NextCrosshair" => "Cycle Crosshair Preset",
            "ExitApp" => "Exit Application",
            _ => action
        };

        private void CancelHotkeyRecording()
        {
            if (_recordingButton != null)
            {
                if (_originalButtonBorder != null) _recordingButton.BorderBrush = _originalButtonBorder;
                if (_originalButtonBackground != null) _recordingButton.Background = _originalButtonBackground;
            }
            _recordingAction = null;
            _recordingButton = null;

            if (HotkeyStatusText != null)
            {
                HotkeyStatusText.Text = "💡 Click any button above, then press your desired key or combination. Press Esc to cancel.";
                HotkeyStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x8C, 0x98, 0xA9));
            }

            UpdateHotkeyUI();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_recordingAction == null || _recordingButton == null) return;

            e.Handled = true;

            InputKey key = (e.Key == InputKey.System) ? e.SystemKey : e.Key;

            if (key == InputKey.Escape)
            {
                CancelHotkeyRecording();
                return;
            }

            // Ignore pure modifier presses
            if (key == InputKey.LeftCtrl || key == InputKey.RightCtrl ||
                key == InputKey.LeftAlt || key == InputKey.RightAlt ||
                key == InputKey.LeftShift || key == InputKey.RightShift ||
                key == InputKey.LWin || key == InputKey.RWin)
            {
                return;
            }

            int vk = KeyInterop.VirtualKeyFromKey(key);
            int modifiers = 0;
            if (Keyboard.IsKeyDown(InputKey.LeftAlt) || Keyboard.IsKeyDown(InputKey.RightAlt)) modifiers |= 1;
            if (Keyboard.IsKeyDown(InputKey.LeftCtrl) || Keyboard.IsKeyDown(InputKey.RightCtrl)) modifiers |= 2;
            if (Keyboard.IsKeyDown(InputKey.LeftShift) || Keyboard.IsKeyDown(InputKey.RightShift)) modifiers |= 4;
            if (Keyboard.IsKeyDown(InputKey.LWin) || Keyboard.IsKeyDown(InputKey.RWin)) modifiers |= 8;

            _currentSettings.EnsureDefaultHotkeys();

            // Check for duplicate assignments
            string? conflictAction = null;
            foreach (var (act, b) in _currentSettings.Hotkeys)
            {
                if (act != _recordingAction && b != null && b.Key == vk && b.ModifierKeys == modifiers)
                {
                    conflictAction = act;
                    break;
                }
            }

            string actionName = _recordingAction;
            _currentSettings.Hotkeys[actionName] = new HotkeyBinding(modifiers, vk);

            if (conflictAction != null)
            {
                _currentSettings.Hotkeys[conflictAction] = new HotkeyBinding(0, 0);
            }

            CancelHotkeyRecording();
            UpdateHotkeyUI();

            if (HotkeyStatusText != null)
            {
                var display = _currentSettings.Hotkeys[actionName].ToDisplayString();
                if (conflictAction != null)
                {
                    HotkeyStatusText.Text = $"✓ Bound to '{display}'. Cleared conflicting key from '{GetActionTitle(conflictAction)}'.";
                }
                else
                {
                    HotkeyStatusText.Text = $"✓ '{GetActionTitle(actionName)}' updated to '{display}'.";
                }
                HotkeyStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x88));
            }

            SettingsService.CurrentInstance.Save();
        }

        private void ResetHotkeys_Click(object sender, RoutedEventArgs e)
        {
            if (_recordingAction != null)
            {
                CancelHotkeyRecording();
            }

            _currentSettings.Hotkeys = AppSettings.GetDefaultHotkeys();
            UpdateHotkeyUI();
            SettingsService.CurrentInstance.Save();

            if (HotkeyStatusText != null)
            {
                HotkeyStatusText.Text = "✓ All hotkeys restored to defaults (F1, F2, F4, Ctrl+Shift+Q).";
                HotkeyStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF));
            }
        }

        private void UpdatePreviewAndAutoSave()
        {
            if (_isInitializing || PreviewCrosshair == null) return;

            var profile = BuildProfileFromUI();
            _currentSettings.Crosshair = profile;

            if (LensWidthSlider != null && LensHeightSlider != null && LensShapeCombo != null)
            {
                _currentSettings.Zoom.ZoomLevel = 2.0;
                _currentSettings.Zoom.Width = (int)LensWidthSlider.Value;
                _currentSettings.Zoom.Height = (int)LensHeightSlider.Value;
                _currentSettings.Zoom.CornerRadius = 16;
                _currentSettings.Zoom.SyncWithCrosshair = SyncPositionCheck?.IsChecked ?? true;

                if (LensShapeCombo.SelectedIndex == 0)
                {
                    _currentSettings.Zoom.Shape = MagnifierShape.WideRectangle;
                    _currentSettings.Zoom.IsCircular = false;
                }
                else if (LensShapeCombo.SelectedIndex == 1)
                {
                    _currentSettings.Zoom.Shape = MagnifierShape.LargeSquare;
                    _currentSettings.Zoom.IsCircular = false;
                }
                else
                {
                    _currentSettings.Zoom.Shape = MagnifierShape.Circular;
                    _currentSettings.Zoom.IsCircular = true;
                    _currentSettings.Zoom.LensSize = (int)LensWidthSlider.Value;
                }
            }

            // Live Preview updates
            PreviewCrosshair.UpdateProfile(profile);

            // Realtime Auto-Save to disk and live in-game overlay
            SettingsService.CurrentInstance.Save();
        }

        private CrosshairProfile BuildProfileFromUI()
        {
            var profile = new CrosshairProfile();

            int idx = ShapeCombo?.SelectedIndex ?? 1;
            profile.Shape = idx switch
            {
                0 => CrosshairShape.Dot,
                1 => CrosshairShape.Cross,
                2 => CrosshairShape.CircleDot,
                3 => CrosshairShape.Crosshair,
                4 => CrosshairShape.Custom,
                _ => CrosshairShape.Cross
            };

            profile.CustomImagePath = _currentSettings.Crosshair?.CustomImagePath;
            profile.Color = string.IsNullOrWhiteSpace(ColorText?.Text) ? "#00FF00" : ColorText.Text.Trim();
            profile.OutlineColor = "#000000";
            profile.Size = SizeSlider?.Value ?? 20;
            profile.Thickness = ThicknessSlider?.Value ?? 2;
            profile.GapSize = GapSlider?.Value ?? 4;
            profile.OffsetX = OffsetXSlider?.Value ?? 0;
            profile.OffsetY = OffsetYSlider?.Value ?? 0;
            profile.Opacity = 1.0;
            profile.ShowOutline = OutlineCheck?.IsChecked ?? true;
            profile.OutlineThickness = 1.5;

            return profile;
        }

        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select Custom Crosshair PNG Image",
                Filter = "PNG Image (*.png)|*.png|All Supported Formats (*.png;*.jpg;*.jpeg;*.ico)|*.png;*.jpg;*.jpeg;*.ico|All Files (*.*)|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog(this) == true && File.Exists(dlg.FileName))
            {
                _currentSettings.Crosshair.CustomImagePath = dlg.FileName;
                CrosshairCanvas.InvalidateImageCache(dlg.FileName);

                if (CustomPathText != null)
                {
                    CustomPathText.Text = Path.GetFileName(dlg.FileName);
                    CustomPathText.ToolTip = dlg.FileName;
                }

                if (ShapeCombo != null)
                {
                    ShapeCombo.SelectedIndex = 4; // Custom PNG
                }

                UpdatePreviewAndAutoSave();
            }
        }

        private void ClearImage_Click(object sender, RoutedEventArgs e)
        {
            _currentSettings.Crosshair.CustomImagePath = null;
            CrosshairCanvas.InvalidateImageCache();

            if (CustomPathText != null)
            {
                CustomPathText.Text = "No custom image selected";
                CustomPathText.ToolTip = null;
            }

            if (ShapeCombo != null && ShapeCombo.SelectedIndex == 4)
            {
                ShapeCombo.SelectedIndex = 1; // Revert to Cross
            }

            UpdatePreviewAndAutoSave();
        }

        private void ColorPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                if (ColorText != null)
                {
                    ColorText.Text = hex;
                }
            }
        }

        private void ResetOffset_Click(object sender, RoutedEventArgs e)
        {
            if (OffsetXSlider != null) OffsetXSlider.Value = 0;
            if (OffsetYSlider != null) OffsetYSlider.Value = 0;
            UpdatePreviewAndAutoSave();
        }

        private void OnCrosshairSettingChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreviewAndAutoSave();
        }

        private void OnCrosshairTextSettingChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePreviewAndAutoSave();
        }

        private void OnCrosshairSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SizeValueLabel != null && SizeSlider != null)
                SizeValueLabel.Text = $"{(int)SizeSlider.Value}px";

            if (ThicknessValueLabel != null && ThicknessSlider != null)
                ThicknessValueLabel.Text = $"{(int)ThicknessSlider.Value}px";

            if (GapValueLabel != null && GapSlider != null)
                GapValueLabel.Text = $"{(int)GapSlider.Value}px";

            if (OffsetXLabel != null && OffsetXSlider != null)
                OffsetXLabel.Text = $"{(int)OffsetXSlider.Value}px";

            if (OffsetYLabel != null && OffsetYSlider != null)
                OffsetYLabel.Text = $"{(int)OffsetYSlider.Value}px";

            UpdatePreviewAndAutoSave();
        }

        private void OnCrosshairCheckChanged(object sender, RoutedEventArgs e)
        {
            UpdatePreviewAndAutoSave();
        }

        private void LensShapeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || LensWidthSlider == null || LensHeightSlider == null) return;

            if (LensShapeCombo.SelectedIndex == 0) // Wide Rectangle
            {
                LensWidthSlider.Value = 650;
                LensHeightSlider.Value = 450;
            }
            else if (LensShapeCombo.SelectedIndex == 1) // Large Square
            {
                LensWidthSlider.Value = 550;
                LensHeightSlider.Value = 550;
            }
            else if (LensShapeCombo.SelectedIndex == 2) // Circular
            {
                LensWidthSlider.Value = 550;
                LensHeightSlider.Value = 550;
            }

            UpdatePreviewAndAutoSave();
        }

        private void LensWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LensWidthLabel != null)
                LensWidthLabel.Text = $"{(int)e.NewValue}px";

            UpdatePreviewAndAutoSave();
        }

        private void LensHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LensHeightLabel != null)
                LensHeightLabel.Text = $"{(int)e.NewValue}px";

            UpdatePreviewAndAutoSave();
        }

        private void OnZoomSettingChanged(object sender, RoutedEventArgs e)
        {
            UpdatePreviewAndAutoSave();
        }

        private void QuickCrosshair_Click(object sender, RoutedEventArgs e)
        {
            ToggleCrosshairRequested?.Invoke();
        }

        private void QuickZoom_Click(object sender, RoutedEventArgs e)
        {
            ToggleZoomRequested?.Invoke();
        }

        private void MinimizeToBackground_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void ExitAppButton_Click(object sender, RoutedEventArgs e)
        {
            ExitAppRequested?.Invoke();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!IsAppExiting)
            {
                e.Cancel = true;
                Hide();
            }
        }
    }
}
