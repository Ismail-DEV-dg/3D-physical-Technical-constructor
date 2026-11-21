using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.App.Controls.ThreeD;

internal static class DisplayMeshes
{
    public static bool Draw(SceneMeshes m, SceneNode n)
    {
        if (n.Family is not ("lcd1602" or "lcd1602-i2c" or "oled-ssd1306" or "seven-segment")) return false;
        var b = n.Body;
        var z = n.Profile.Clearance;
        var h = n.Profile.Height;
        var top = z + h;
        var detail = n.Profile.DetailHeight;
        m.Box(b, z, h, n.Profile.Material);
        if (n.Family == "seven-segment")
        {
            Segment(m, b, top, .23, .14, .54, .067, 0);
            Segment(m, b, top, .75, .20, .09, .27, 1);
            Segment(m, b, top, .75, .53, .09, .27, 2);
            Segment(m, b, top, .23, .80, .54, .067, 3);
            Segment(m, b, top, .16, .53, .09, .27, 4);
            Segment(m, b, top, .16, .20, .09, .27, 5);
            Segment(m, b, top, .23, .47, .54, .067, 6);
            m.Disc(b.X + b.Width * .88, b.Y + b.Height * .84, top, .12, b.Width * .075, "#593440", "segment-7");
            return true;
        }
        var oled = n.Family == "oled-ssd1306";
        var frame = BoardMeshes.Relative(b, .07, .22, .86, .67);
        var glass = BoardMeshes.Relative(frame, .06, .12, .88, .74);
        m.Box(frame, top, detail * .64, "#101824");
        m.Box(glass, top + detail * .64, detail * .10, oled ? "#0B1628" : "#294A43");
        m.Text(glass, top + detail * .74, "", "#B3FFF0", oled ? "#0B1628" : "#294A43", "screen");
        if (n.Family == "lcd1602-i2c") m.Box(BoardMeshes.Relative(b, .12, .01, .28, .18), top, detail * .30, "#1A7257");
        foreach (var pin in n.Contacts)
            m.Disc(pin.Position.X, pin.Position.Y, top, .25, 1.2, SceneMeshes.Gold, sides: 8);
        return true;
    }

    private static void Segment(SceneMeshes m, LayoutRect b, double z, double x, double y, double w, double h, int i) =>
        m.Box(BoardMeshes.Relative(b, x, y, w, h), z, .12, "#593440", $"segment-{i}");
}
