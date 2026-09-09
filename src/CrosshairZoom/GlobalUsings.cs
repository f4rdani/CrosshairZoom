// Resolve ambiguity between System.Drawing (WinForms) and System.Windows (WPF)
global using Point = System.Windows.Point;
global using Size = System.Windows.Size;
global using Pen = System.Windows.Media.Pen;
global using Brush = System.Windows.Media.Brush;
global using Brushes = System.Windows.Media.Brushes;
global using Color = System.Windows.Media.Color;
global using Application = System.Windows.Application;
global using MessageBox = System.Windows.MessageBox;
global using InputKey = System.Windows.Input.Key;
