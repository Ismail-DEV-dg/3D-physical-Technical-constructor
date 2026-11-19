using System.Windows;
using System.Windows.Media;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.App.Controls;

internal static class ServoDrawing
{
    public static void Draw(DrawingContext dc, FootprintDefinition footprint)
    {
        var parts = footprint.Parts!;
        var body = PhysicalDrawing.Rect(parts["case"]);
        var connector = PhysicalDrawing.Rect(parts["connector"]);
        string[] colors = ["#ED9935", "#C83F46", "#543C2E"];
        for (var i = 0; i < footprint.Contacts.Count; i++)
        {
            var contact = footprint.Contacts[i];
            var cable = new StreamGeometry();
            using (var path = cable.Open())
            {
                path.BeginFigure(new Point(body.Right - .5, body.Bottom - 2 + i * .9), false, false);
                path.BezierTo(new Point(body.Right + 11, body.Bottom + i), new Point(connector.Left - 9, contact.Y),
                    new Point(connector.Left + 1, contact.Y), true, false);
            }
            dc.DrawGeometry(null, new Pen(PhysicalDrawing.Ink("#20252C"), 1.05), cable);
            dc.DrawGeometry(null, new Pen(PhysicalDrawing.Ink(colors[i]), .7), cable);
        }
        var plastic = new LinearGradientBrush(PhysicalDrawing.Ink("#2D7EC4").Color, PhysicalDrawing.Ink("#17436C").Color, 70);
        foreach (var key in new[] { "left-ear", "right-ear" })
        {
            var ear = PhysicalDrawing.Rect(parts[key]);
            dc.DrawRoundedRectangle(plastic, new Pen(PhysicalDrawing.Ink("#123855"), .18), ear, .3, .3);
            BoardDetailDrawing.Hole(dc, new Point(ear.Left + ear.Width / 2, ear.Top + ear.Height / 2), .75);
        }
        dc.DrawRoundedRectangle(plastic, new Pen(PhysicalDrawing.Ink("#0A2E50"), .3), body, .8, .8);
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#3379B7"), new Pen(PhysicalDrawing.Ink("#5C9CD1"), .15),
            new Rect(body.Left + 1, body.Top + 1, body.Width - 2, body.Height - 2), .6, .6);
        var shaft = PhysicalDrawing.Rect(parts["shaft"]);
        var center = new Point(shaft.Left + shaft.Width / 2, shaft.Top + shaft.Height / 2);
        dc.DrawEllipse(PhysicalDrawing.Ink("#EBEBDE"), new Pen(Brushes.Gray, .2), center, shaft.Width / 2, shaft.Height / 2);
        PhysicalDrawing.Text(dc, "SG90", body.Left + 12, body.Top + 8.5, 1.6, Brushes.White);
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#232A32"), new Pen(Brushes.Black, .18), connector, .4, .4);
        foreach (var contact in footprint.Contacts)
        {
            PhysicalDrawing.Lead(dc, new Point(contact.X - 1, contact.Y), new Point(contact.X + 1, contact.Y));
            PhysicalDrawing.Contact(dc, new Point(contact.X, contact.Y));
            PhysicalDrawing.Text(dc, contact.ElectricalPinId, connector.Left + .4, contact.Y - 1.2, BoardDetailDrawing.PinLabelSize, Brushes.WhiteSmoke);
        }
    }

    internal static void DrawArm(DrawingContext dc, FootprintDefinition footprint, double angle)
    {
        var shaft = PhysicalDrawing.Rect(footprint.Parts!["shaft"]);
        var center = new Point(shaft.Left + shaft.Width / 2, shaft.Top + shaft.Height / 2);
        dc.PushTransform(new RotateTransform(angle, center.X, center.Y));
        dc.DrawRoundedRectangle(Brushes.WhiteSmoke, new Pen(Brushes.Gray, .15), new Rect(center.X - 2, center.Y - 1.1, 16, 2.2), 1, 1);
        for (var i = 0; i < 4; i++) dc.DrawEllipse(PhysicalDrawing.Ink("#586471"), null, new Point(center.X + 4 + i * 2.5, center.Y), .35, .35);
        dc.DrawEllipse(PhysicalDrawing.Metal, new Pen(Brushes.Gray, .1), center, .9, .9);
        dc.Pop();
    }
}
