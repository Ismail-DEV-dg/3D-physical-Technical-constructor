using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.App.Controls.ThreeD;

internal static class BoardMeshes
{
    public static void Breadboard(SceneMeshes m, SceneNode node, CancellationToken token)
    {
        var b = node.Body;
        var h = node.Profile.Height;
        var d = node.Profile.DetailHeight;
        m.Box(b, 0, h, node.Profile.Material);
        m.Box(SceneMeshes.Center(b, b.Width - 2, 3), h, d, "#A7ADB6");
        foreach (var rail in new[] { (3.2, "#CF485C"), (6.1, "#367DC1") })
        {
            m.Box(new(b.X + 2, b.Y + rail.Item1, b.Width - 4, .24), h, d, rail.Item2);
            m.Box(new(b.X + 2, b.Bottom - rail.Item1, b.Width - 4, .24), h, d, rail.Item2);
        }
        foreach (var contact in node.Contacts)
        {
            token.ThrowIfCancellationRequested();
            m.Box(new(contact.Position.X - .41, contact.Position.Y - .41, .82, .82), h, d, "#343B44");
        }
    }

    public static bool Draw(SceneMeshes m, SceneNode n)
    {
        if (n.Layer != SceneLayer.Board && n.Family != "dip-chip") return false;
        var b = n.Body;
        var z = n.Profile.Clearance;
        var h = n.Profile.Height;
        var top = z + h;
        var detail = n.Profile.DetailHeight;
        m.Box(b, z, h, n.Profile.Material);
        if (n.Family == "dip-chip")
        {
            m.Disc(b.X + b.Width / 2, b.Y + .6, top, detail, b.Width * .23, "#10121A");
            m.Disc(b.X + .9, b.Y + 1.5, top, detail, .7, "#76787E");
            m.Text(SceneMeshes.Center(b, b.Width - 1, Math.Min(4, b.Height * .4)), top, n.Name, "#E3E8ED", role: "mark");
            return true;
        }

        var chip = Relative(b, .33, .29, .35, .42);
        m.Box(chip, top, detail * .19, "#181B23");
        m.Text(chip, top + detail * .19, n.Family == "esp32-devkit" ? "ESP32" : "ATMEL", "#CDD9D5", role: "mark");
        m.Box(Relative(b, .02, .30, .20, .32), top, detail * .73, SceneMeshes.Metal);
        m.Box(Relative(b, .02, .36, .06, .20), top + detail * .20, detail * .34, "#303842");
        if (n.Family == "arduino-uno")
        {
            m.Box(Relative(b, .02, .74, .19, .19), top, detail * .67, "#252932");
            m.Box(Relative(b, .25, .67, .08, .10), top, detail * .52, "#A1A6B0");
        }
        if (n.Family == "esp32-devkit")
        {
            var antenna = Relative(b, .73, .22, .24, .56);
            m.Box(antenna, top, detail * .12, "#151920");
            for (var i = 0; i < 5; i++)
                m.Box(Relative(antenna, .15 + i * .14, .15, .045, .70), top + detail * .12, .15, SceneMeshes.Gold);
        }
        foreach (var c in n.Contacts)
        {
            m.Box(new(c.Position.X - .94, c.Position.Y - .94, 1.88, 1.88), top, detail * .22, "#161B25");
            m.Box(new(c.Position.X - .34, c.Position.Y - .34, .68, .68), top + detail * .22, .12, SceneMeshes.Gold);
        }
        m.Box(Relative(b, .71, .57, .05, .06), top, .34, "#379662", "board-led");
        return true;
    }

    public static LayoutRect Relative(LayoutRect r, double x, double y, double w, double h) =>
        new(r.X + r.Width * x, r.Y + r.Height * y, r.Width * w, r.Height * h);
}
