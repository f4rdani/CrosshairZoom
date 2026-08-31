namespace CrosshairZoom.Models;

public enum MagnifierShape
{
    WideRectangle,
    LargeSquare,
    Circular
}

public class ZoomProfile
{
    public double ZoomLevel { get; set; } = 2.0;
    public int Width { get; set; } = 650;      // default wide lens width
    public int Height { get; set; } = 450;     // default wide lens height
    public int LensSize { get; set; } = 550;   // for circular size
    public bool IsCircular { get; set; } = false; // default to Wide Rectangle
    public MagnifierShape Shape { get; set; } = MagnifierShape.WideRectangle;
    public int CornerRadius { get; set; } = 16;
    public double RefreshRate { get; set; } = 60;
    public int OffsetX { get; set; } = 0; // Horizontal offset (-left / +right)
    public int OffsetY { get; set; } = 0; // Vertical offset (-up / +down)
    public bool SyncWithCrosshair { get; set; } = true;
}
