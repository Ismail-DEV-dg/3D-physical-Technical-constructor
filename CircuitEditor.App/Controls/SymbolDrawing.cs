using System.Windows;
using System.Windows.Media;

namespace CircuitEditor.App.Controls;

// Normalised vector symbols, drawn from primitives; no model or simulation logic.
internal static class SymbolDrawing
{
    public static void Draw(DrawingContext context, string symbol, Brush ink, Rect target)
    {
        var pen = new Pen(ink, 1.6);
        var path = symbol switch
        {
            "resistor" => "M 0,18 L 10,18 15,7 23,29 31,7 39,29 47,7 53,18 64,18",
            "capacitor" => "M 0,18 L 26,18 M 26,4 L 26,32 M 38,4 L 38,32 M 38,18 L 64,18",
            "inductor" => "M 0,18 L 8,18 C 8,-4 20,-4 20,18 C 20,-4 32,-4 32,18 C 32,-4 44,-4 44,18 C 44,-4 56,-4 56,18 L 64,18",
            "potentiometer" => "M 0,18 L 14,18 14,7 50,7 50,29 14,29 14,18 M 50,18 L 64,18 M 32,0 L 32,16 M 27,11 L 32,16 37,11",
            "led" => "M 0,18 L 20,18 M 20,6 L 20,30 42,18 Z M 44,6 L 44,30 M 44,18 L 64,18 M 39,2 L 49,-8 M 44,-8 L 49,-8 49,-3 M 51,5 L 61,-5 M 56,-5 L 61,-5 61,0",
            "diode" => "M 0,18 L 20,18 M 20,6 L 20,30 42,18 Z M 44,6 L 44,30 M 44,18 L 64,18",
            "battery" => "M 0,18 L 22,18 M 22,0 L 22,36 M 34,9 L 34,27 M 34,18 L 64,18 M 12,0 L 12,8 M 8,4 L 16,4",
            "switch" => "M 0,22 L 14,22 M 14,22 L 48,5 M 48,22 L 64,22",
            "npn" => "M 0,18 L 24,18 M 24,3 L 24,33 M 24,12 L 46,0 64,0 M 24,24 L 46,36 64,36 M 36,27 L 46,36 34,35",
            "buzzer" => "M 4,10 L 22,10 38,0 38,36 22,26 4,26 Z M 45,8 Q 55,18 45,28 M 54,0 Q 69,18 54,36",
            "servo" => "M 12,0 L 52,0 52,36 12,36 Z M 0,18 L 64,18 M 32,6 L 32,30",
            "display" => "M 0,0 L 64,0 64,36 0,36 Z M 8,10 L 56,10 M 8,18 L 56,18 M 8,26 L 42,26",
            "relay" => "M 0,18 L 8,18 8,5 28,5 28,31 8,31 8,18 M 28,18 L 34,18 M 40,28 L 58,6 M 58,28 L 64,28",
            _ => "M 10,0 L 54,0 54,36 10,36 Z M 0,10 L 10,10 M 0,26 L 10,26 M 54,10 L 64,10 M 54,26 L 64,26 M 24,10 L 40,10 40,26 24,26 Z"
        };
        var geometry = Geometry.Parse(path);
        var bounds = geometry.GetRenderBounds(pen);
        const double strokeClearance = 2;
        var scale = Math.Min((target.Width - 2 * strokeClearance) / bounds.Width,
            (target.Height - 2 * strokeClearance) / bounds.Height);
        context.PushTransform(new TranslateTransform(target.X + (target.Width - bounds.Width * scale) / 2,
            target.Y + (target.Height - bounds.Height * scale) / 2));
        context.PushTransform(new ScaleTransform(scale, scale));
        context.PushTransform(new TranslateTransform(-bounds.X, -bounds.Y));
        context.DrawGeometry(null, pen, geometry);
        context.Pop(); context.Pop(); context.Pop();
    }
}
