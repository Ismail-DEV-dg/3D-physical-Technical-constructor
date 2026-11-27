using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CircuitEditor.App.Controls;

internal sealed class StartupStars
{
    private const int StarCount = 2400;
    private const int BrightCount = 44;
    private const int BrightnessLevels = 32;
    private const int ColorsCount = 3;
    private static readonly Color[] Tints =
        [Color.FromRgb(231, 239, 255), Color.FromRgb(176, 210, 255), Color.FromRgb(245, 220, 199)];
    private static readonly (double X, double Y, double Radius, double Opacity, int Tint, double Phase, bool Glow)[] Field = CreateField();
    private static readonly Brush[][] BrightColors = Tints.Select(tint => Enumerable.Range(0, BrightnessLevels + 1)
        .Select(level => Frozen(Color.FromArgb(Alpha(level), tint.R, tint.G, tint.B))).ToArray()).ToArray();
    private static readonly Brush[] Halos = Enumerable.Range(0, BrightnessLevels + 1).Select(CreateHalo).ToArray();
    private BitmapSource? background;
    private Size backgroundSize;

    public void Draw(DrawingContext context, Size size, double phase)
    {
        var scale = Math.Clamp(Math.Min(size.Width / 850, size.Height / 480), .72, 1.32);
        if (background is null || backgroundSize != size)
        {
            backgroundSize = size;
            background = Bake(size, scale);
        }
        context.DrawImage(background, new Rect(size));
        foreach (var star in Field.Take(BrightCount).Where(star => star.Glow))
        {
            var point = new Point(star.X * size.Width, star.Y * size.Height);
            var radius = star.Radius * scale;
            var twinkle = .80 + .20 * Math.Sin(star.Phase + phase * (5 + star.Tint));
            var level = (int)Math.Round(star.Opacity * twinkle * BrightnessLevels);
            if (level == 0) continue;
            context.DrawEllipse(Halos[level], null, point, radius * 4.7, radius * 4.7);
            context.DrawEllipse(BrightColors[star.Tint][level], null, point, radius, radius);
        }
    }

    private static BitmapSource Bake(Size size, double scale)
    {
        var width = (int)Math.Ceiling(size.Width);
        var height = (int)Math.Ceiling(size.Height);
        var stride = width * 4;
        var pixels = new byte[stride * height];
        foreach (var star in Field.Where(point => !point.Glow && point.Opacity > 0))
        {
            var cx = star.X * width;
            var cy = star.Y * height;
            var radius = star.Radius * scale;
            var tint = Tints[star.Tint];
            var left = Math.Max(0, (int)Math.Floor(cx - radius));
            var right = Math.Min(width - 1, (int)Math.Floor(cx + radius));
            var top = Math.Max(0, (int)Math.Floor(cy - radius));
            var bottom = Math.Min(height - 1, (int)Math.Floor(cy + radius));
            for (var y = top; y <= bottom; y++)
                for (var x = left; x <= right; x++)
                {
                    var dx = x + .5 - cx;
                    var dy = y + .5 - cy;
                    var coverage = Math.Clamp(radius + .5 - Math.Sqrt(dx * dx + dy * dy), 0, 1);
                    var alpha = coverage * star.Opacity;
                    var index = y * stride + x * 4;
                    pixels[index] = Blend(tint.B, pixels[index], alpha);
                    pixels[index + 1] = Blend(tint.G, pixels[index + 1], alpha);
                    pixels[index + 2] = Blend(tint.R, pixels[index + 2], alpha);
                    pixels[index + 3] = Blend(255, pixels[index + 3], alpha);
                }
        }
        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Pbgra32, null, pixels, stride);
        bitmap.Freeze();
        return bitmap;
    }

    private static byte Blend(byte source, byte destination, double alpha) =>
        (byte)Math.Round(source * alpha + destination * (1 - alpha));

    private static (double, double, double, double, int, double, bool)[] CreateField()
    {
        var random = new Random(42139);
        var points = new (double, double, double, double, int, double, bool)[StarCount];
        for (var index = 0; index < points.Length; index++)
        {
            var x = random.NextDouble();
            var y = .016 + random.NextDouble() * .54;
            if (index >= StarCount * .43)
                y = StartupStarCloud.Band(x) + (random.NextDouble() + random.NextDouble() + random.NextDouble() - 1.5) * .19;
            var bright = index < BrightCount && y < .45;
            var radius = bright ? .64 + random.NextDouble() * .70 : .29 + Math.Pow(random.NextDouble(), 2) * .54;
            var horizon = Math.Clamp((.55 - y) / .27, 0, 1);
            var opacity = horizon * (bright ? .66 + random.NextDouble() * .30 : .24 + random.NextDouble() * .51);
            points[index] = (x, y, radius, opacity, random.Next(ColorsCount), random.NextDouble() * Math.Tau, bright);
        }
        return points;
    }

    private static byte Alpha(int level) => (byte)(255 * level / BrightnessLevels);

    private static Brush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Brush CreateHalo(int level)
    {
        var brush = new RadialGradientBrush(
            Color.FromArgb((byte)(132 * level / BrightnessLevels), 157, 193, 249), Color.FromArgb(0, 157, 193, 249));
        brush.Freeze();
        return brush;
    }
}
