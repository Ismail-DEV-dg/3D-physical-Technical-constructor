using System.Windows.Media;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.App.Controls.ThreeD;

internal static class ControllerMeshes
{
    private const double HeaderWidth = 2.28;
    private const string SocketColor = "#19222B";
    private const string Microcontroller = "#1E252D";

    public static bool Draw(SceneMeshes m, SceneNode n)
    {
        if (n.Layer != SceneLayer.Board || n.Footprint is not { } footprint) return false;
        var b = n.Body;
        Action<DrawingContext>? paint = n.Family switch
        {
            "arduino-uno" => dc => ArduinoUnoDrawing.Draw(dc, footprint),
            "arduino-nano" => dc => ArduinoNanoDrawing.Draw(dc, footprint),
            "esp32-devkit" => dc => Esp32Drawing.Draw(dc, footprint),
            _ => null
        };
        if (paint is null) return false;

        var top = n.Profile.Clearance + n.Profile.Height;
        var detail = n.Profile.DetailHeight;
        var pcb = n.Family == "arduino-nano" ? b with { Y = b.Y + 3, Height = b.Height - 3 } : b;
        m.BeveledBox(pcb, n.Profile.Clearance, n.Profile.Height, n.Profile.Material, corner: .9, bevel: .18);
        var artwork = new SceneSurfaceTexture(b, paint);
        m.Face(b, top, artwork);
        var center = b.X + b.Width / 2;
        switch (n.Family)
        {
            case "arduino-uno":
                Uno(m, top, detail, artwork, footprint);
                break;
            case "arduino-nano":
                Raised(m, new(center - 3.8, b.Y, 7.6, 6.5), top, detail * .78, SceneMeshes.Metal, artwork);
                Raised(m, new(center - 3, 15, 6, 6), top, detail * .24, Microcontroller, artwork);
                Raised(m, new(center - 2.2, 28, 4.4, 4), top, detail * .21, Microcontroller, artwork);
                Raised(m, new(center - 1.5, 6, 3, 3), top, detail * .34, "#7F8689", artwork);
                var led = ArduinoNanoDrawing.BuiltinLed(footprint);
                Indicator(m, led.X, led.Y, 1.4, .7, top);
                break;
            case "esp32-devkit":
                Raised(m, new(center - 7.7, 2, 15.4, 22), top, detail * .54, SceneMeshes.Metal, artwork);
                Raised(m, new(center - 4, b.Bottom - 5.5, 8, 5.5), top, detail * .67, SceneMeshes.Metal, artwork);
                Raised(m, new(center - 2.5, 27, 5, 5), top, detail * .24, Microcontroller, artwork);
                foreach (var x in new[] { center - 7, center + 7 })
                    Raised(m, new(x - 1.6, 36.8, 3.2, 2.8), top, detail * .35, "#535E66", artwork);
                break;
        }
        return true;
    }

    private static void Uno(SceneMeshes m, double top, double detail, SceneSurfaceTexture artwork, FootprintDefinition footprint)
    {
        Raised(m, new(0, 14, 11, 12), top, detail * .83, SceneMeshes.Metal, artwork);
        Raised(m, new(.5, 35, 11, 9), top, detail * .70, SocketColor, artwork);
        Raised(m, new(27, 28, 34, 7.7), top, detail * .28, Microcontroller, artwork);
        Raised(m, new(14, 18, 7, 7), top, detail * .16, Microcontroller, artwork);
        Raised(m, new(15, 11, 6, 4), top, detail * .25, "#545F64", artwork);
        var led = ArduinoUnoDrawing.BuiltinLed;
        Indicator(m, led.X, led.Y, 1.5, .8, top);

        var groups = footprint.Contacts.GroupBy(c => c.Y < 20
            ? c.X < 39 ? "digital-low" : "digital-high"
            : c.ElectricalPinId.StartsWith('A') ? "analog" : "power");
        foreach (var group in groups)
        {
            var left = group.Min(c => c.X) - HeaderWidth / 2;
            var right = group.Max(c => c.X) + HeaderWidth / 2;
            Raised(m, new(left, group.First().Y - HeaderWidth / 2, right - left, HeaderWidth),
                top, detail * .51, SocketColor, artwork);
        }
    }

    private static void Raised(SceneMeshes m, LayoutRect area, double top, double height, string color, SceneSurfaceTexture artwork)
    {
        m.BeveledBox(area, top, height, color, corner: .32, bevel: .15);
        m.Face(area, top + height, artwork);
    }

    private static void Indicator(SceneMeshes m, double x, double y, double width, double height, double top) =>
        m.BeveledBox(new(x, y, width, height), top, .24, "#D8BE55", corner: .09, bevel: .06, role: "board-led");
}
