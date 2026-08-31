using System;
using System.Windows;
using CrosshairZoom.Models;
using CrosshairZoom.Windows;

namespace CrosshairZoom.Services
{
    public class OverlayService
    {
        private OverlayWindow? _overlayWindow;

        public bool IsVisible => _overlayWindow != null && _overlayWindow.IsVisible;

        public void SetOverlayWindow(OverlayWindow window)
        {
            _overlayWindow = window;
        }

        public void Show()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_overlayWindow != null)
                {
                    _overlayWindow.Show();
                    _overlayWindow.WindowState = WindowState.Maximized;
                }
            });
        }

        public void Hide()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                _overlayWindow?.Hide();
            });
        }

        public void Toggle()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public void UpdateCrosshair(CrosshairProfile profile)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                _overlayWindow?.UpdateCrosshair(profile);
            });
        }
    }
}
