using System.Windows;
using System.Windows.Media;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.App.Controls;

internal static class ProximitySensorDrawing
{
    public static void Ultrasound(DrawingContext dc, FootprintDefinition footprint)
    {
        SensorPinDrawing.StraightLeads(dc, footprint);
        var body = PhysicalDrawing.Rect(footprint.Body);
        BoardDetailDrawing.Pcb(dc, body, "#246DAD");
        foreach (var key in new[] { "left-eye", "right-eye" })
        {
            var eye = PhysicalDrawing.Rect(footprint.Parts![key]);
            var center = new Point(eye.Left + eye.Width / 2, eye.Top + eye.Height / 2);
            dc.DrawEllipse(PhysicalDrawing.Metal, new Pen(Brushes.DimGray, .22), center, eye.Width / 2, eye.Height / 2);
            dc.DrawEllipse(PhysicalDrawing.Ink("#35404C"), new Pen(Brushes.Silver, .25), center, eye.Width * .41, eye.Height * .41);
            dc.PushClip(new EllipseGeometry(center, eye.Width * .39, eye.Height * .39));
            var mesh = new Pen(PhysicalDrawing.Ink("#84949D"), .12);
            for (var step = 1; step < 16; step++)
            {
                dc.DrawLine(mesh, new Point(eye.Left + step, eye.Top), new Point(eye.Left + step, eye.Bottom));
                dc.DrawLine(mesh, new Point(eye.Left, eye.Top + step), new Point(eye.Right, eye.Top + step));
            }
            dc.Pop();
        }
        BoardDetailDrawing.Chip(dc, new Rect(body.Left + body.Width / 2 - 2.5, body.Top + 6, 5, 5), "IC");
        BoardDetailDrawing.TextCentered(dc, "HC-SR04", body.Left + body.Width / 2, body.Bottom - 1.7, 1.2, Brushes.WhiteSmoke);
        SensorPinDrawing.Draw(dc, footprint);
    }

    public static void Pir(DrawingContext dc, FootprintDefinition footprint)
    {
        SensorPinDrawing.StraightLeads(dc, footprint);
        var body = PhysicalDrawing.Rect(footprint.Body);
        BoardDetailDrawing.Pcb(dc, body, "#23694B");
        var lens = PhysicalDrawing.Rect(footprint.Parts!["lens"]);
        var center = new Point(lens.Left + lens.Width / 2, lens.Top + lens.Height / 2);
        dc.DrawEllipse(new RadialGradientBrush(Colors.White, Color.FromRgb(210, 222, 222)),
            new Pen(PhysicalDrawing.Ink("#8A9A9D"), .18), center, lens.Width / 2, lens.Height / 2);
        var facets = new Pen(PhysicalDrawing.Ink("#B7C4C6"), .1);
        for (var ring = 1; ring < 5; ring++)
            dc.DrawEllipse(null, facets, center, lens.Width / 2 * ring / 5, lens.Height / 2 * ring / 5);
        dc.PushClip(new EllipseGeometry(center, lens.Width * .49, lens.Height * .49));
        for (var index = 0; index < 12; index++)
        {
            var angle = index * Math.PI / 6;
            dc.DrawLine(facets, center, new Point(center.X + Math.Cos(angle) * lens.Width / 2, center.Y + Math.Sin(angle) * lens.Height / 2));
        }
        dc.Pop();
        SensorPinDrawing.Draw(dc, footprint);
    }
}
