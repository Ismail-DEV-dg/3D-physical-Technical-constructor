using System.Windows;
using System.Windows.Media;

namespace CircuitEditor.App.Controls;

internal static class EnvironmentPassiveDrawing
{
    public static void Draw(DrawingContext dc, Rect body, bool ntc)
    {
        var center = new Point(body.Left + body.Width / 2, body.Top + body.Height / 2);
        dc.DrawEllipse(new LinearGradientBrush(PhysicalDrawing.Ink(ntc ? "#5D7188" : "#EBCEA0").Color,
            PhysicalDrawing.Ink(ntc ? "#273B51" : "#A37549").Color, 55), new Pen(PhysicalDrawing.Ink("#403A32"), .15),
            center, body.Width / 2, body.Height / 2);
        if (ntc)
        {
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(80, 235, 250, 255)), null,
                new Point(center.X - .5, center.Y - .5), .45, .3);
            BoardDetailDrawing.TextCentered(dc, "10k", center.X, center.Y + .1, 1.2, Brushes.WhiteSmoke);
        }
        else
        {
            var trace = new StreamGeometry();
            using (var path = trace.Open())
            {
                var left = body.Left + 1; var right = body.Right - 1;
                path.BeginFigure(new Point(left, body.Top + 1), false, false);
                for (var index = 0; index < 6; index++)
                {
                    var y = body.Top + 1 + index * (body.Height - 2) / 5;
                    path.LineTo(new Point(index % 2 == 0 ? right : left, y), true, false);
                    if (index < 5) path.LineTo(new Point(index % 2 == 0 ? right : left, y + (body.Height - 2) / 5), true, false);
                }
            }
            dc.DrawGeometry(null, new Pen(PhysicalDrawing.Ink("#954C35"), .22), trace);
        }
    }
}
