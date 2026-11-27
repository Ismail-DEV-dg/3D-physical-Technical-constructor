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
        const double substrate = .24;
        m.BeveledBox(pcb, n.Profile.Clearance, substrate, "#79684A", corner: .95, bevel: .07);
        m.BeveledBox(pcb, n.Profile.Clearance + substrate, n.Profile.Height - substrate,
            n.Profile.Material, corner: .95, bevel: .16);
        var artwork = new SceneSurfaceTexture(b, paint);
        m.Face(b, top, artwork);
        var center = b.X + b.Width / 2;
        switch (n.Family)
        {
            case "arduino-uno":
                Uno(m, top, detail, artwork, footprint);
                break;
            case "arduino-nano":
                Port(m, new(center - 3.8, b.Y, 7.6, 6.5), top, detail * .78, leftFacing: false);
                Raised(m, new(center - 3, 15, 6, 6), top, detail * .24, Microcontroller, artwork);
                Raised(m, new(center - 2.2, 28, 4.4, 4), top, detail * .21, Microcontroller, artwork);
                Raised(m, new(center - 1.5, 6, 3, 3), top, detail * .34, "#7F8689", artwork);
                var led = ArduinoNanoDrawing.BuiltinLed(footprint);
                Indicator(m, led.X, led.Y, 1.4, .7, top);
                MaleHeaders(m, n, top);
                break;
            case "esp32-devkit":
                Raised(m, new(center - 7.7, 2, 15.4, 22), top, detail * .54, SceneMeshes.Metal, artwork);
                Port(m, new(center - 4, b.Bottom - 5.5, 8, 5.5), top, detail * .67, leftFacing: false, reverse: true);
                Raised(m, new(center - 2.5, 27, 5, 5), top, detail * .24, Microcontroller, artwork);
                foreach (var x in new[] { center - 7, center + 7 })
                    Raised(m, new(x - 1.6, 36.8, 3.2, 2.8), top, detail * .35, "#535E66", artwork);
                MaleHeaders(m, n, top);
                break;
        }
        return true;
    }

    private static void Uno(SceneMeshes m, double top, double detail, SceneSurfaceTexture artwork, FootprintDefinition footprint)
    {
        Port(m, new(-.25, 14, 11.25, 12), top, detail * .83, leftFacing: true);
        Port(m, new(.5, 35, 11, 9), top, detail * .70, leftFacing: true, power: true);
        Raised(m, new(27, 28, 34, 7.7), top, detail * .28, Microcontroller, artwork);
        for (var i = 0; i < 14; i++)
        {
            var x = 28.4 + 2.38 * i;
            foreach (var y in new[] { 27.1, 35.5 })
                m.BeveledBox(new(x, y, .48, 1.05), top + .18, .46, SceneMeshes.Metal, corner: .07, bevel: .08);
        }
        Raised(m, new(14, 18, 7, 7), top, detail * .16, Microcontroller, artwork);
        Raised(m, new(15, 11, 6, 4), top, detail * .25, "#545F64", artwork);
        var led = ArduinoUnoDrawing.BuiltinLed;
        Indicator(m, led.X, led.Y, 1.5, .8, top);

        var groups = footprint.Contacts.GroupBy(c => c.Y < 20
            ? c.X < 39 ? "digital-low" : "digital-high"
            : c.ElectricalPinId.StartsWith('A') ? "analog" : "power");
        foreach (var group in groups)
        {
            var pins = group.OrderBy(c => c.X).ToArray();
            var baseX = pins[0].X - HeaderWidth / 2;
            var end = pins[^1].X + HeaderWidth / 2;
            var baseY = pins[0].Y - HeaderWidth / 2;
            var height = SceneContactDepth.UnoSocketHeight;
            var floor = height - SceneContactDepth.UnoSocketBore;
            var header = new LayoutRect(baseX, baseY, end - baseX, HeaderWidth);
            m.Box(header, top, floor - SceneMeshes.RecessBackingClearance, SocketColor);
            m.SideWalls(header, top + floor - SceneMeshes.RecessBackingClearance, top + height, SocketColor);
            for (var i = 0; i < pins.Length; i++)
            {
                var p = pins[i];
                var right = i + 1 == pins.Length ? end : (p.X + pins[i + 1].X) / 2;
                const double opening = 1.30;
                m.Recess(new(baseX, baseY, right - baseX, HeaderWidth),
                    new(p.X - opening / 2, p.Y - opening / 2, opening, opening),
                    top + height, SceneContactDepth.UnoSocketBore, SocketColor, "#353E46", "#181713");
                m.Ring(p.X, p.Y, top + floor + .04, .13, .91, .62, SceneMeshes.Gold);
                baseX = right;
            }
        }
    }

    private static void MaleHeaders(SceneMeshes m, SceneNode node, double top)
    {
        foreach (var pin in node.Contacts)
        {
            var x = pin.Position.X;
            var y = pin.Position.Y;
            m.Ring(x, y, top, .16, 1.74, .8, SceneMeshes.Gold);
            m.BeveledBox(new(x - .58, y - .58, 1.16, 1.16), top + .11, .64, SocketColor, corner: .13, bevel: .12);
        }
    }

    private static void Port(SceneMeshes m, LayoutRect box, double z, double height, bool leftFacing,
        bool reverse = false, bool power = false)
    {
        const double wall = .42;
        const double back = .65;
        var color = power ? SocketColor : SceneMeshes.Metal;
        m.Box(box, z, wall, color);
        m.BeveledBox(box, z + height - wall, wall, color, corner: .22, bevel: .09,
            finish: power ? SceneFinish.Matte : SceneFinish.Metal);
        if (leftFacing)
        {
            m.Box(new(box.X, box.Y, box.Width, wall), z, height - wall, color);
            m.Box(new(box.X, box.Bottom - wall, box.Width, wall), z, height - wall, color);
            m.Box(new(box.Right - back, box.Y, back, box.Height), z, height - wall, "#202831");
            if (power)
                m.Rod(new(box.Right - back, box.Y + box.Height / 2, z + height / 2),
                    new(box.X + 1.2, box.Y + box.Height / 2, z + height / 2), 1.26, SceneMeshes.Metal);
            else
                m.Box(SceneMeshes.Inset(box, 1.2, 1.9), z + height * .32, .65, "#2A323B");
        }
        else
        {
            m.Box(new(box.X, box.Y, wall, box.Height), z, height - wall, color);
            m.Box(new(box.Right - wall, box.Y, wall, box.Height), z, height - wall, color);
            m.Box(new(box.X, reverse ? box.Y : box.Bottom - back, box.Width, back), z, height - wall, "#202831");
            m.Box(SceneMeshes.Inset(box, 1.0, 1.1), z + height * .36, .44, "#2A323B");
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
