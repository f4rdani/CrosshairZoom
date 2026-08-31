namespace CrosshairZoom.Models;

public enum CrosshairShape
{
    Dot,
    Cross,
    CircleDot,
    Crosshair,  // cross + dot
    Custom
}

public class CrosshairProfile
{
    public CrosshairShape Shape { get; set; } = CrosshairShape.Cross;
    public string Color { get; set; } = "#00FF00";  // Green
    public double Size { get; set; } = 20;
    public double Thickness { get; set; } = 2;
    public double Opacity { get; set; } = 1.0;
    public double GapSize { get; set; } = 4;  // Center gap
    public bool ShowOutline { get; set; } = true;
    public string OutlineColor { get; set; } = "#000000";
    public double OutlineThickness { get; set; } = 1.5;
    public double OffsetX { get; set; } = 0; // Horizontal shift (-left / +right)
    public double OffsetY { get; set; } = 0; // Vertical shift (-up / +down)
    public string? CustomImagePath { get; set; }
}
