using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Inputs;

namespace CircuitEditor.App.Controls;

internal static class MiscComponentDrawing
{
    public static void Switch(DrawingContext dc, ComponentViewModel item)
    {
        var footprint = item.Footprint;
        SensorPinDrawing.StraightLeads(dc, footprint);
        var body = PhysicalDrawing.Rect(footprint.Body); var slot = PhysicalDrawing.Rect(footprint.Parts!["slot"]);
        dc.DrawRoundedRectangle(PhysicalDrawing.Metal, new Pen(Brushes.DimGray, .2), body, .5, .5);
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#17202A"), new Pen(Brushes.Black, .12), slot, .3, .3);
        SwitchControl(dc, footprint, item.Model.Inputs.Values[ComponentInput.SwitchPosition]);
        SensorPinDrawing.Draw(dc, footprint);
    }

    internal static void SwitchControl(DrawingContext dc, Core.Physical.FootprintDefinition footprint, double position)
    {
        var slot = PhysicalDrawing.Rect(footprint.Parts!["slot"]);
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#17202A"), new Pen(Brushes.Black, .12), slot, .3, .3);
        const double width = 3.5;
        var x = position == 0 ? slot.Left + .4 : slot.Right - width - .4;
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#3F4F61"), new Pen(Brushes.SlateGray, .1), new Rect(x, slot.Top + .3, width, slot.Height - .6), .3, .3);
        for (var i = 1; i < 4; i++) dc.DrawLine(new Pen(Brushes.LightGray, .12), new Point(x + i * .7, slot.Top + .65), new Point(x + i * .7, slot.Bottom - .65));
    }

    public static void Battery(DrawingContext dc, ComponentViewModel item)
    {
        var footprint = item.Footprint; var cell = PhysicalDrawing.Rect(footprint.Parts!["cell"]);
        for (var index = 0; index < footprint.Contacts.Count; index++)
        {
            var contact = footprint.Contacts[index]; var wire = new StreamGeometry();
            using (var path = wire.Open())
            {
                path.BeginFigure(new Point(cell.Right - 1, cell.Top + 3 + index * 10), false, false);
                path.BezierTo(new Point(cell.Right + 6, cell.Top + 3 + index * 10), new Point(-8, contact.Y), new Point(-1, contact.Y), true, false);
            }
            dc.DrawGeometry(null, new Pen(PhysicalDrawing.Ink("#1D2530"), 1), wire);
            dc.DrawGeometry(null, new Pen(PhysicalDrawing.Ink(index == 0 ? "#D54E56" : "#394657"), .65), wire);
            PhysicalDrawing.Lead(dc, new Point(-1, contact.Y), new Point(contact.X, contact.Y));
            PhysicalDrawing.Contact(dc, new Point(contact.X, contact.Y));
            BoardDetailDrawing.ExternalPinLabel(dc, contact.ElectricalPinId, contact.X + 2.2, contact.Y + (index == 0 ? -2 : 0));
        }
        var voltage = ComponentInputDefinitions.For("battery")[0].Format(item.Model.Inputs.Values[ComponentInput.BatteryVoltage]);
        BatteryFace(dc, cell, voltage);
    }

    internal static void BatteryFace(DrawingContext dc, Rect cell, string voltage)
    {
        dc.DrawRoundedRectangle(new LinearGradientBrush(PhysicalDrawing.Ink("#343F4E").Color, PhysicalDrawing.Ink("#17202B").Color, 90),
            new Pen(Brushes.Black, .22), cell, .8, .8);
        dc.DrawRoundedRectangle(PhysicalDrawing.Metal, new Pen(Brushes.DimGray, .15), new Rect(cell.Right - 3, cell.Top, 3, cell.Height), .5, .5);
        BoardDetailDrawing.TextCentered(dc, voltage, cell.Left + cell.Width / 2 - 1, cell.Top + 6, 3.5, Brushes.WhiteSmoke);
        PhysicalDrawing.Text(dc, "+", cell.Right - 6, cell.Top + 1, 2, Brushes.OrangeRed);
        PhysicalDrawing.Text(dc, "−", cell.Right - 6, cell.Bottom - 3.5, 2, Brushes.LightGray);
    }
}
