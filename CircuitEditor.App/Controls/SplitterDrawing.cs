using System.Windows;
using System.Windows.Media;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.App.Controls;

internal static class SplitterDrawing
{
    internal static void Draw(DrawingContext dc, FootprintDefinition footprint)
    {
        var body = PhysicalDrawing.Rect(footprint.Body);
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#123633"), new Pen(PhysicalDrawing.Ink("#83A99E"), .15), body, 1.1, 1.1);
        var cap = new Rect(body.X + .28, body.Y + .21, body.Width - .56, body.Height - .66);
        dc.DrawRoundedRectangle(new LinearGradientBrush(Color.FromRgb(67, 145, 127), Color.FromRgb(33, 91, 80), 90),
            new Pen(PhysicalDrawing.Ink("#628F83"), .12), cap, .87, .87);
        dc.DrawLine(new Pen(PhysicalDrawing.Ink("#92B9AB"), .16), new(body.X + 1.1, body.Y + .6), new(body.Right - 1.1, body.Y + .6));
        foreach (var contact in footprint.Contacts)
        {
            var point = new Point(contact.X, contact.Y);
            var input = contact.Id == "IN";
            dc.DrawEllipse(PhysicalDrawing.Ink(input ? "#DFC784" : "#C0B479"), new Pen(PhysicalDrawing.Ink("#7A7047"), .14), point, 1.33, 1.33);
            dc.DrawEllipse(PhysicalDrawing.Ink("#172323"), new Pen(PhysicalDrawing.Ink("#595743"), .13), point, .84, .84);
        }
        Markings(dc, footprint.Contacts);
    }

    internal static void Markings(DrawingContext dc, IReadOnlyList<ContactDefinition> contacts)
    {
        for (var index = 0; index < contacts.Count; index++)
        {
            var pin = contacts[index];
            PhysicalDrawing.Text(dc, index == 0 ? "IN" : index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                pin.X - (index == 0 ? .68 : .32), pin.Y + 1.58, 1.16, PhysicalDrawing.Ink(index == 0 ? "#F9DC97" : "#DBF3E9"));
        }
    }
}
