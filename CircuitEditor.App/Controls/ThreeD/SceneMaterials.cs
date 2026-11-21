using System.Collections.Concurrent;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace CircuitEditor.App.Controls.ThreeD;

internal static class SceneMaterials
{
    private const double FontSize = 24;
    private const double Margin = 3;
    private static readonly ConcurrentDictionary<string, Material> Solids = new(StringComparer.OrdinalIgnoreCase);

    public static Material Solid(string color) => Solids.GetOrAdd(color, key =>
    {
        DiffuseMaterial material = new(Brush(key));
        material.Freeze();
        return material;
    });

    public static Material Texture(string value, string foreground, string background, double aspect)
    {
        var text = new FormattedText(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Consolas"), FontSize, Brush(foreground), 1);
        var bounds = new Rect(0, 0, Math.Max(1, aspect * (FontSize + Margin * 2)), FontSize + Margin * 2);
        var scale = Math.Min(1, Math.Min((bounds.Width - 2 * Margin) / Math.Max(1, text.Width),
            (bounds.Height - 2 * Margin) / Math.Max(1, text.Height)));
        DrawingGroup drawing = new();
        using (var context = drawing.Open())
        {
            context.DrawRectangle(Brush(background), null, bounds);
            if (scale > 0)
            {
                context.PushTransform(new ScaleTransform(scale, scale, bounds.Width / 2, bounds.Height / 2));
                context.DrawText(text, new((bounds.Width - text.Width) / 2, (bounds.Height - text.Height) / 2));
            }
        }
        DrawingBrush brush = new(drawing) { Stretch = Stretch.Fill };
        DiffuseMaterial material = new(brush);
        material.Freeze();
        return material;
    }

    public static SolidColorBrush Brush(string value)
    {
        Color color;
        try { color = (Color)ColorConverter.ConvertFromString(value); }
        catch (FormatException) { color = Colors.SlateGray; }
        SolidColorBrush brush = new(color);
        brush.Freeze();
        return brush;
    }
}
