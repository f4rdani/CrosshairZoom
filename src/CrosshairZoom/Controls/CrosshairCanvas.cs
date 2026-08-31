using System;
using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CrosshairZoom.Models;

namespace CrosshairZoom.Controls
{
    public class CrosshairCanvas : FrameworkElement
    {
        private static readonly ConcurrentDictionary<string, ImageSource?> _imageCache = new();

        public static readonly DependencyProperty ProfileProperty =
            DependencyProperty.Register(
                "Profile", 
                typeof(CrosshairProfile), 
                typeof(CrosshairCanvas), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public CrosshairProfile? Profile
        {
            get => (CrosshairProfile?)GetValue(ProfileProperty);
            set => SetValue(ProfileProperty, value);
        }

        public static void InvalidateImageCache(string? path = null)
        {
            if (string.IsNullOrEmpty(path))
                _imageCache.Clear();
            else
                _imageCache.TryRemove(path, out _);
        }

        public void UpdateProfile(CrosshairProfile? profile)
        {
            Profile = profile;
            InvalidateVisual();
        }

        private static ImageSource? GetImageSource(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;

            return _imageCache.GetOrAdd(path, p =>
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(p, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
                catch
                {
                    return null;
                }
            });
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            double s = Profile != null ? Math.Max(Profile.Size * 2 + 20, 60) : 60;
            return new Size(s, s);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (Profile == null) return;

            double w = ActualWidth > 0 ? ActualWidth : (Width > 0 ? Width : 120);
            double h = ActualHeight > 0 ? ActualHeight : (Height > 0 ? Height : 120);
            double cx = (w / 2.0) + (w > 300 ? Profile.OffsetX : Math.Clamp(Profile.OffsetX * 0.3, -40, 40));
            double cy = (h / 2.0) + (h > 300 ? Profile.OffsetY : Math.Clamp(Profile.OffsetY * 0.3, -40, 40));

            // Custom PNG Image rendering
            if (Profile.Shape == CrosshairShape.Custom || !string.IsNullOrWhiteSpace(Profile.CustomImagePath))
            {
                if (!string.IsNullOrWhiteSpace(Profile.CustomImagePath))
                {
                    var img = GetImageSource(Profile.CustomImagePath);
                    if (img != null)
                    {
                        double renderSize = Math.Max(6.0, Profile.Size);
                        Rect destRect = new Rect(cx - (renderSize / 2.0), cy - (renderSize / 2.0), renderSize, renderSize);

                        if (Profile.Opacity < 1.0)
                        {
                            dc.PushOpacity(Math.Clamp(Profile.Opacity, 0.05, 1.0));
                            dc.DrawImage(img, destRect);
                            dc.Pop();
                        }
                        else
                        {
                            dc.DrawImage(img, destRect);
                        }
                        return;
                    }
                }
            }

            Color mainColor = Colors.LimeGreen;
            Color outlineColor = Colors.Black;

            try
            {
                if (!string.IsNullOrWhiteSpace(Profile.Color))
                    mainColor = (Color)System.Windows.Media.ColorConverter.ConvertFromString(Profile.Color.Trim());
            }
            catch { }

            try
            {
                if (!string.IsNullOrWhiteSpace(Profile.OutlineColor))
                    outlineColor = (Color)System.Windows.Media.ColorConverter.ConvertFromString(Profile.OutlineColor.Trim());
            }
            catch { }

            SolidColorBrush mainBrush = new SolidColorBrush(mainColor) { Opacity = Math.Clamp(Profile.Opacity, 0.05, 1.0) };
            SolidColorBrush outlineBrush = new SolidColorBrush(outlineColor) { Opacity = Math.Clamp(Profile.Opacity, 0.05, 1.0) };

            double thickness = Math.Max(1.0, Profile.Thickness);
            double outlineWidth = Math.Max(1.0, Profile.OutlineThickness > 0 ? Profile.OutlineThickness : 1.5);
            double totalOutlineThickness = thickness + (outlineWidth * 2.0);

            Pen mainPen = new Pen(mainBrush, thickness)
            {
                StartLineCap = PenLineCap.Square,
                EndLineCap = PenLineCap.Square
            };

            Pen outlinePen = new Pen(outlineBrush, totalOutlineThickness)
            {
                StartLineCap = PenLineCap.Square,
                EndLineCap = PenLineCap.Square
            };

            double gap = Math.Max(0.0, Profile.GapSize);
            double size = Math.Max(2.0, Profile.Size);
            double halfSize = size / 2.0;
            double dotRadius = Math.Max(1.5, thickness);

            // 1. Draw Outline layer first if enabled
            if (Profile.ShowOutline)
            {
                switch (Profile.Shape)
                {
                    case CrosshairShape.Dot:
                        dc.DrawEllipse(outlineBrush, null, new Point(cx, cy), dotRadius + outlineWidth, dotRadius + outlineWidth);
                        break;

                    case CrosshairShape.Cross:
                        dc.DrawLine(outlinePen, new Point(cx, cy - gap), new Point(cx, cy - (gap + halfSize)));
                        dc.DrawLine(outlinePen, new Point(cx, cy + gap), new Point(cx, cy + (gap + halfSize)));
                        dc.DrawLine(outlinePen, new Point(cx - gap, cy), new Point(cx - (gap + halfSize), cy));
                        dc.DrawLine(outlinePen, new Point(cx + gap, cy), new Point(cx + (gap + halfSize), cy));
                        break;

                    case CrosshairShape.CircleDot:
                        dc.DrawEllipse(null, outlinePen, new Point(cx, cy), halfSize, halfSize);
                        dc.DrawEllipse(outlineBrush, null, new Point(cx, cy), dotRadius + outlineWidth, dotRadius + outlineWidth);
                        break;

                    case CrosshairShape.Crosshair:
                        dc.DrawLine(outlinePen, new Point(cx, cy - gap), new Point(cx, cy - (gap + halfSize)));
                        dc.DrawLine(outlinePen, new Point(cx, cy + gap), new Point(cx, cy + (gap + halfSize)));
                        dc.DrawLine(outlinePen, new Point(cx - gap, cy), new Point(cx - (gap + halfSize), cy));
                        dc.DrawLine(outlinePen, new Point(cx + gap, cy), new Point(cx + (gap + halfSize), cy));
                        dc.DrawEllipse(outlineBrush, null, new Point(cx, cy), dotRadius + outlineWidth, dotRadius + outlineWidth);
                        break;
                }
            }

            // 2. Draw Main layer on top
            switch (Profile.Shape)
            {
                case CrosshairShape.Dot:
                    dc.DrawEllipse(mainBrush, null, new Point(cx, cy), dotRadius, dotRadius);
                    break;

                case CrosshairShape.Cross:
                    dc.DrawLine(mainPen, new Point(cx, cy - gap), new Point(cx, cy - (gap + halfSize)));
                    dc.DrawLine(mainPen, new Point(cx, cy + gap), new Point(cx, cy + (gap + halfSize)));
                    dc.DrawLine(mainPen, new Point(cx - gap, cy), new Point(cx - (gap + halfSize), cy));
                    dc.DrawLine(mainPen, new Point(cx + gap, cy), new Point(cx + (gap + halfSize), cy));
                    break;

                case CrosshairShape.CircleDot:
                    dc.DrawEllipse(null, mainPen, new Point(cx, cy), halfSize, halfSize);
                    dc.DrawEllipse(mainBrush, null, new Point(cx, cy), dotRadius, dotRadius);
                    break;

                case CrosshairShape.Crosshair:
                    dc.DrawLine(mainPen, new Point(cx, cy - gap), new Point(cx, cy - (gap + halfSize)));
                    dc.DrawLine(mainPen, new Point(cx, cy + gap), new Point(cx, cy + (gap + halfSize)));
                    dc.DrawLine(mainPen, new Point(cx - gap, cy), new Point(cx - (gap + halfSize), cy));
                    dc.DrawLine(mainPen, new Point(cx + gap, cy), new Point(cx + (gap + halfSize), cy));
                    dc.DrawEllipse(mainBrush, null, new Point(cx, cy), dotRadius, dotRadius);
                    break;
            }
        }
    }
}
