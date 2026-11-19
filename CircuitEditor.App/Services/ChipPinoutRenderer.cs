using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CircuitEditor.App.Controls;
using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.App.Services;

public sealed class ChipPinoutRenderer : IChipPinoutRenderer
{
    private const double PixelsPerMm = 15;
    private const double Margin = 28;
    private const double LabelSize = 15;
    private const double TitleSize = 24;
    private const double HeaderHeight = 84;
    private const double LabelGap = 10;
    private const double Leader = 10;
    private const double MaximumLabelWidth = 340;
    private const double Dpi = 96;
    private static readonly Brush Ink = PhysicalDrawing.Ink("#172434");
    private static readonly Brush Muted = PhysicalDrawing.Ink("#52657C");

    public byte[] Render(ChipDefinition definition)
    {
        var footprint = ChipPackage.Footprint(definition);
        var pins = definition.Pins.ToDictionary(pin => ChipPackage.PinId(pin.Number));
        var labels = footprint.Contacts.ToDictionary(contact => contact.Id, contact =>
        {
            var pin = pins[contact.ElectricalPinId];
            return Format($"{pin.Number}  {pin.Name}  ·  {(pin.IsUnused ? "NC" : ChipMarkdown.TypeName(pin.Type))}", LabelSize, Ink);
        });
        var leftWidth = SideWidth(footprint, labels, true);
        var rightWidth = SideWidth(footprint, labels, false);
        var packageWidth = footprint.Contacts.Max(point => point.X) * PixelsPerMm;
        var title = Format(definition.Name, TitleSize, Ink);
        var width = (int)Math.Ceiling(Math.Max(title.Width + Margin * 2,
            Margin * 2 + leftWidth + rightWidth + packageWidth + 2 * (LabelGap + Leader)));
        var height = (int)Math.Ceiling(HeaderHeight + footprint.Body.Height * PixelsPerMm + Margin);

        var left = Margin + leftWidth + LabelGap + Leader;
        var top = HeaderHeight - footprint.Body.Y * PixelsPerMm;
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
            dc.DrawText(title, new Point(Margin, 14));
            dc.DrawText(Format($"{definition.Package}  ·  {definition.Family}  ·  ревизия {definition.DefinitionVersion}",
                LabelSize, Muted), new Point(Margin, 48));
            dc.PushTransform(new TranslateTransform(left, top));
            dc.PushTransform(new ScaleTransform(PixelsPerMm, PixelsPerMm));
            ChipPackageDrawing.Draw(dc, footprint, definition.Name);
            dc.Pop();
            dc.Pop();

            var pen = new Pen(Muted, 1);
            foreach (var contact in footprint.Contacts)
            {
                var leftSide = contact.X < footprint.Body.X;
                var bodyX = left + contact.X * PixelsPerMm;
                var y = top + contact.Y * PixelsPerMm;
                var end = bodyX + (leftSide ? -Leader : Leader);
                var label = labels[contact.Id];
                dc.DrawLine(pen, new Point(bodyX, y), new Point(end, y));
                dc.DrawText(label, new Point(leftSide ? end - LabelGap - label.Width : end + LabelGap, y - label.Height / 2));
            }
        }

        var bitmap = new RenderTargetBitmap(width, height, Dpi, Dpi, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static double SideWidth(FootprintDefinition footprint, Dictionary<string, FormattedText> labels, bool left) =>
        footprint.Contacts.Where(contact => (contact.X < footprint.Body.X) == left)
            .Max(contact => labels[contact.Id].Width);

    private static FormattedText Format(string text, double size, Brush brush)
    {
        var formatted = WpfTextMeasurer.Format(text, size, brush);
        formatted.MaxTextWidth = MaximumLabelWidth;
        formatted.MaxLineCount = 1;
        formatted.Trimming = TextTrimming.CharacterEllipsis;
        return formatted;
    }
}
