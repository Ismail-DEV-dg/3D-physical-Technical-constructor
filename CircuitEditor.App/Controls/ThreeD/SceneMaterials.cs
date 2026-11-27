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
    private const double LabelTextureScale = 3;
    private static readonly ConcurrentDictionary<string, Material> Solids = new(StringComparer.OrdinalIgnoreCase);

    private static readonly ConcurrentDictionary<(string Color, SceneFinish Finish), Material> Finishes = new();
    private static readonly ConcurrentDictionary<(string Base, string Color, SceneFinish Finish), Material[]> Emission = new();

    public static Material Glow(string body, string color, int brightness, SceneFinish finish = SceneFinish.Matte) =>
        Emission.GetOrAdd((body, color, finish), key =>
        {
            var baseMaterial = Finished(key.Base, key.Finish);
            var shade = Brush(key.Color).Color;
            var levels = new Material[Core.Scene.SceneSignalStyle.BrightnessLevels];
            levels[0] = baseMaterial;
            for (var i = 1; i < levels.Length; i++)
            {
                var tint = shade;
                tint.A = (byte)Math.Round(255d * i / (levels.Length - 1));
                var brush = new SolidColorBrush(tint);
                var material = new MaterialGroup();
                material.Children.Add(baseMaterial);
                material.Children.Add(new EmissiveMaterial(brush));
                material.Freeze();
                levels[i] = material;
            }
            return levels;
        })[brightness];

    public static Material Solid(string color) => Solids.GetOrAdd(color, key =>
    {
        DiffuseMaterial material = new(Brush(key));
        material.Freeze();
        return material;
    });

    public static Material Finished(string color, SceneFinish finish)
    {
        if (finish == SceneFinish.Matte) return Solid(color);
        return Finishes.GetOrAdd((color, finish), key =>
        {
            var metal = key.Finish == SceneFinish.Metal;
            var group = new MaterialGroup();
            group.Children.Add(Solid(key.Color));
            group.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(metal ? (byte)104 : (byte)65, 242, 241, 239)),
                metal ? 19 : 30));
            group.Freeze();
            return group;
        });
    }

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
        return SceneTextureRasterizer.Create(drawing, bounds, LabelTextureScale);
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
