using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BrandingGenerator;

internal static class BrandRenderer
{
    public const string White = "#EAF2FF";
    public const string Muted = "#A8BADE";
    public static SolidColorBrush Ink(string hex) => new((Color)ColorConverter.ConvertFromString(hex));

    public static BitmapSource Render(int width, int height, Action<DrawingContext> paint)
    {
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen()) paint(context);
        var target = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        target.Render(visual);
        return target;
    }

    public static void DrawMark(DrawingContext context, Mark mark, double x, double y, double size)
    {
        var scale = size / mark.Size;
        context.PushTransform(new MatrixTransform(scale, 0, 0, scale, x, y));
        var bounds = new Rect(0.7, 0.7, mark.Size - 1.4, mark.Size - 1.4);
        context.DrawRoundedRectangle(Ink(MarkCatalog.Background), new Pen(Ink("#334260"), size <= 32 ? 1.15 : 1.8),
            bounds, mark.CornerRadius, mark.CornerRadius);
        foreach (var trace in mark.Traces)
        {
            var pen = new Pen(Ink(trace.Color), trace.Width)
            {
                StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round
            };
            context.DrawGeometry(null, pen, Geometry.Parse(trace.Data));
        }
        foreach (var node in mark.Nodes)
            context.DrawEllipse(Ink(node.Color), null, new Point(node.X, node.Y), node.Radius, node.Radius);
        context.Pop();
    }

    public static void Label(DrawingContext context, string text, double x, double y, double size,
        string color = White, bool bold = true)
    {
        var face = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
        var formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, face, size, Ink(color), 1);
        context.DrawText(formatted, new Point(x, y));
    }

    public static void Fill(DrawingContext context, int width, int height)
    {
        // Quantize the gradient explicitly so output does not depend on WPF dithering.
        var from = Ink("#0A0E17").Color;
        var to = Ink("#181635").Color;
        for (var y = 0; y < height; y++)
        {
            var position = y / (double)Math.Max(1, height - 1);
            byte Channel(byte first, byte last) => (byte)Math.Round(first + (last - first) * position);
            var color = Color.FromRgb(Channel(from.R, to.R), Channel(from.G, to.G), Channel(from.B, to.B));
            context.DrawRectangle(new SolidColorBrush(color), null, new Rect(0, y, width, 1));
        }
    }
}
