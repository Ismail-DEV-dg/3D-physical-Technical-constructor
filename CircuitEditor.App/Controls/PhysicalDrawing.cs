using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.Services;
using CircuitEditor.Core.Layout;

namespace CircuitEditor.App.Controls;

internal static class PhysicalDrawing
{
    public static SolidColorBrush Ink(string color) => new((Color)ColorConverter.ConvertFromString(color));
    public static Rect Rect(LayoutRect value) => new(value.X, value.Y, value.Width, value.Height);
    public static void Text(DrawingContext dc, string text, double x, double y, double mmSize, Brush brush) =>
        dc.DrawText(WpfTextMeasurer.Format(text, mmSize, brush), new Point(x, y));
    public static void TextRight(DrawingContext dc, string text, double right, double y, double mmSize, Brush brush)
    {
        var formatted = WpfTextMeasurer.Format(text, mmSize, brush);
        dc.DrawText(formatted, new Point(right - formatted.WidthIncludingTrailingWhitespace, y));
    }
    public static LinearGradientBrush Metal { get; } = new(Colors.Silver, Colors.WhiteSmoke, 90);
    private const double LeadOutlineWidth = .65;
    private const double LeadMetalWidth = .43;
    private static readonly Brush LeadOutline = Ink("#20252C");
    private static readonly Brush LeadMetal = new LinearGradientBrush
    {
        StartPoint = new Point(0, 0), EndPoint = new Point(0, 1),
        GradientStops = new GradientStopCollection
        {
            new(Ink("#3B424B").Color, 0), new(Ink("#A0A8B0").Color, .35),
            new(Ink("#505965").Color, .6), new(Ink("#303740").Color, 1)
        }
    };
    public static void Lead(DrawingContext dc, Point from, Point to)
    {
        dc.DrawLine(new Pen(LeadOutline, LeadOutlineWidth), from, to);
        dc.DrawLine(new Pen(LeadMetal, LeadMetalWidth), from, to);
    }
    public static void Contact(DrawingContext dc, Point point) =>
        dc.DrawEllipse(LeadMetal, new Pen(LeadOutline, .14), point, .35, .35);
}
