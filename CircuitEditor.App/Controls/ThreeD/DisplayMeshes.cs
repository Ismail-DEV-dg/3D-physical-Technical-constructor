using System.Windows;
using System.Windows.Media;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.App.Controls.ThreeD;

internal static class DisplayMeshes
{
    public static bool Draw(SceneMeshes m, SceneNode n)
    {
        if (n.Family is not ("lcd1602" or "lcd1602-i2c" or "oled-ssd1306" or "seven-segment") ||
            n.Footprint is not { } footprint) return false;
        var b = n.Body;
        var z = n.Profile.Clearance;
        var h = n.Profile.Height;
        var top = z + h;
        var detail = n.Profile.DetailHeight;
        var body = PhysicalDrawing.Rect(b);
        var oled = n.Family == "oled-ssd1306";
        var segments = n.Family == "seven-segment";
        m.BeveledBox(b, z, h, segments ? "#CFD3D6" : n.Profile.Material, bevel: .18);
        Action<DrawingContext> paint = n.Family switch
        {
            "seven-segment" => dc => SevenSegmentDrawing.Draw(dc, footprint),
            "oled-ssd1306" => dc => OledDrawing.Draw(dc, footprint),
            _ => dc => Lcd1602Drawing.Draw(dc, footprint)
        };
        var art = new SceneSurfaceTexture(b, paint);
        m.Face(b, top, art);
        if (segments)
        {
            DrawSegments(m, body, top);
            return true;
        }

        var frame = oled ? OledDrawing.GlassBounds(body) : Lcd1602Drawing.FrameBounds(body);
        var frameTop = top + detail * .54;
        m.BeveledBox(Rect(frame), top, frameTop - top, "#101923", corner: .45, bevel: .24);
        m.Face(Rect(frame), frameTop, art);
        var screen = Rect(oled ? frame : Lcd1602Drawing.ScreenBounds(frame));
        // The separate screen uses the full UV square so its material can be replaced during playback.
        m.Face(screen, frameTop + .045, art, "screen");
        if (n.Family == "lcd1602-i2c")
        {
            LayoutRect adapter = new(b.X + 22, b.Y + .8, 39, 6.4);
            m.BeveledBox(adapter, top, detail * .22, "#203A34", corner: .27, bevel: .11);
            m.Face(adapter, top + detail * .22, art);
        }
        foreach (var pin in n.Contacts)
            if (!b.Contains(new LayoutPoint(pin.Position.X, pin.Position.Y)))
                m.Disc(pin.Position.X, pin.Position.Y, pin.Position.Z, .22, 1.15, SceneMeshes.Gold, sides: 12);
        return true;
    }

    private static void DrawSegments(SceneMeshes m, Rect body, double z)
    {
        var panel = SevenSegmentDrawing.FaceBounds(body);
        var center = panel.Left + panel.Width / 2 - .5;
        var top = panel.Top + 1;
        var middle = panel.Top + panel.Height / 2;
        var bottom = panel.Bottom - 1;
        Horizontal(0, top);
        Vertical(1, center + 3.1, true);
        Vertical(2, center + 3.1, false);
        Horizontal(3, bottom);
        Vertical(4, center - 3.1, false);
        Vertical(5, center - 3.1, true);
        Horizontal(6, middle);
        m.Disc(panel.Right - .8, bottom, z + .025, .06, 1.1, "#94434B", "segment-7");
        void Horizontal(int bit, double y) => Polygon(bit, new(center - 2.5, y), new(center + 2.5, y));
        void Vertical(int bit, double x, bool upper)
        {
            if (upper) Polygon(bit, new(x, top + .8), new(x - .25, middle - .8));
            else Polygon(bit, new(x - .25, middle + .8), new(x - .5, bottom - .8));
        }
        void Polygon(int bit, Point a, Point b) => m.Plate(SevenSegmentDrawing.SegmentContour(a, b), z + .025, .06,
            "#94434B", $"segment-{bit}");
    }

    private static LayoutRect Rect(Rect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);
}
