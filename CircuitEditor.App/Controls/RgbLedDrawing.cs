using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Inputs;

namespace CircuitEditor.App.Controls;

internal static class RgbLedDrawing
{
    public static void Draw(DrawingContext dc, ComponentViewModel item)
    {
        var footprint = item.Footprint;
        var body = PhysicalDrawing.Rect(footprint.Parts!["lens"]);
        var center = new Point(body.Left + body.Width / 2, body.Top + body.Height / 2);
        string[] knees = ["r-knee", "common-knee", "g-knee", "b-knee"];
        for (var index = 0; index < footprint.Contacts.Count; index++)
        {
            var contact = footprint.Contacts[index]; var knee = footprint.Parts[knees[index]];
            PhysicalDrawing.Lead(dc, new Point(center.X + (index - 1.5) * .6, body.Bottom - 1), new Point(contact.X, knee.Y));
            PhysicalDrawing.Lead(dc, new Point(contact.X, knee.Y), new Point(contact.X, contact.Y));
        }
        dc.DrawEllipse(PhysicalDrawing.Ink("#B1BFC9"), new Pen(Brushes.DimGray, .15), new Point(center.X, body.Bottom - .4), body.Width / 2 + .25, .75);
        dc.DrawEllipse(new LinearGradientBrush(Color.FromArgb(190, 235, 248, 253), Color.FromArgb(180, 115, 142, 161), 45),
            new Pen(PhysicalDrawing.Ink("#637D91"), .16), center, body.Width / 2, body.Height / 2);
        var colors = new[] { "#F56872", "#69CD87", "#5B99E6" };
        for (var index = 0; index < colors.Length; index++)
            dc.DrawEllipse(PhysicalDrawing.Ink(colors[index]), new Pen(Brushes.DimGray, .08),
                new Point(center.X - .9 + index * .9, center.Y + .3), .4, .5);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)), null, new Point(center.X - .8, center.Y - 1), .55, .8);
        var anode = item.Model.Inputs.Values[ComponentInput.RgbCommon] == 1;
        foreach (var contact in footprint.Contacts)
        {
            PhysicalDrawing.Contact(dc, new Point(contact.X, contact.Y));
            BoardDetailDrawing.ExternalPinLabel(dc, contact.ElectricalPinId == "COM" ? anode ? "A" : "K" : contact.ElectricalPinId,
                contact.X, contact.Y + .6);
        }
    }
}
