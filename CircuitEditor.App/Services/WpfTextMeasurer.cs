using System.Globalization;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Media;
using CircuitEditor.Core.Layout;

namespace CircuitEditor.App.Services;

public sealed class WpfTextMeasurer : ITextMeasurer
{
    public const double InkPadding = 1;
    public static FontFamily LabelFont { get; } = new(
        PackUriHelper.Create(new Uri("application:///")), "/CircuitEditor.App;component/Resources/Fonts/#JetBrains Mono");
    public static FormattedText Format(string text, double size, Brush brush, double pixelsPerDip = 1) =>
        new(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface(LabelFont, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
            size, brush, pixelsPerDip);

    public TextExtent Measure(string text, double fontSize)
    {
        var formatted = Format(text.Length == 0 ? " " : text, fontSize, Brushes.Black);
        return new TextExtent(
            Math.Ceiling(formatted.WidthIncludingTrailingWhitespace +
                Math.Max(0, formatted.OverhangLeading) + Math.Max(0, formatted.OverhangTrailing)) + 2 * InkPadding,
            Math.Ceiling(formatted.Height + Math.Max(0, formatted.OverhangAfter)) + 2 * InkPadding);
    }
}
