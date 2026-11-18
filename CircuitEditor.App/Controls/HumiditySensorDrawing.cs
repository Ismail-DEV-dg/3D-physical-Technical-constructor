using System.Windows;
using System.Windows.Media;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.App.Controls;

internal static class HumiditySensorDrawing
{
    public static void Draw(DrawingContext dc, FootprintDefinition footprint)
    {
        SensorPinDrawing.StraightLeads(dc, footprint);
        var body = PhysicalDrawing.Rect(footprint.Body);
        var white = footprint.Renderer == "dht22";
        var light = white ? "#FAFAED" : "#409EDD";
        var dark = white ? "#CED3CD" : "#1B6397";
        dc.DrawRoundedRectangle(new LinearGradientBrush(PhysicalDrawing.Ink(light).Color, PhysicalDrawing.Ink(dark).Color, 45),
            new Pen(PhysicalDrawing.Ink(white ? "#8E999F" : "#17446A"), .2), body, .7, .7);
        BoardDetailDrawing.TextCentered(dc, white ? "DHT22" : "DHT11", body.Left + body.Width / 2, body.Top + 1.2,
            2, white ? Brushes.DarkSlateGray : Brushes.WhiteSmoke);
        const int columns = 4;
        var rows = white ? 10 : 6;
        var cellWidth = (body.Width - 3) / columns;
        var cellHeight = (body.Height - 6.5) / rows;
        var recess = PhysicalDrawing.Ink(white ? "#849296" : "#123F63");
        for (var row = 0; row < rows; row++)
            for (var col = 0; col < columns; col++)
                dc.DrawRoundedRectangle(recess, null, new Rect(body.Left + 1.5 + col * cellWidth,
                    body.Top + 5 + row * cellHeight, cellWidth - .55, cellHeight - .45), .18, .18);
        SensorPinDrawing.Draw(dc, footprint);
    }
}
