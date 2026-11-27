using System.Windows.Media;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.App.Controls.ThreeD;

internal static class DeviceMeshes
{
    public static bool Draw(SceneMeshes m, SceneNode n)
    {
        var b = n.Body;
        var z = n.Profile.Clearance;
        var h = n.Profile.Height;
        var top = z + h;
        var detail = n.Profile.DetailHeight;
        var color = n.Profile.Material;
        var x = b.X + b.Width / 2;
        var y = b.Y + b.Height / 2;
        var d = Math.Min(b.Width, b.Height);
        switch (n.Family)
        {
            case "button":
                m.BeveledBox(b, z, h * .88, "#292F37", corner: .55);
                m.BeveledBox(SceneMeshes.Inset(b,.18,.18), z + h * .88, h * .12, SceneMeshes.Metal, corner: .44);
                foreach (var dx in new[] { b.X + 1, b.Right - 1 })
                    foreach (var dy in new[] { b.Y + 1, b.Bottom - 1 })
                        m.Disc(dx, dy, top, .09, .53, "#59646C", sides: 12);
                m.Disc(x, y, top, .24, d * .68, "#131920");
                m.Disc(x, y, top - .22, detail + .22, d * .55, "#333C47", "button", finish: SceneFinish.Gloss);
                break;
            case "switch":
                var slot = n.Parts.GetValueOrDefault("slot", SceneMeshes.Center(b, b.Width * .75, b.Height * .5));
                m.BeveledBox(b, z, h - .72 - SceneMeshes.RecessBackingClearance, SceneMeshes.Metal, corner: .5, bevel: .22);
                m.SideWalls(b, top - .90, top, "#74818A");
                m.Recess(b, slot, top, .72, "#74818A", "#929DA3", "#161B25");
                m.BeveledBox(SceneMeshes.Center(slot, slot.Width * .34, slot.Height * .80), top - .48, detail + .48,
                    "#536273", corner: .31, bevel: .22, role: "switch", pivot: new(slot.Width * .27, 0, 0));
                break;
            case "potentiometer":
                var knob = n.Knob ?? new(x, y);
                var knobD = n.KnobRadius > 0 ? n.KnobRadius * 2 : d * .72;
                m.BeveledBox(b, z, h, "#33685F", corner: .8);
                m.Text(new(b.X, b.Y, b.Width, 1.9), top, n.Value, "#E7EEE5", role: "mark");
                m.Disc(knob.X, knob.Y, top, detail * .32, knobD, SceneMeshes.Metal);
                m.Disc(knob.X, knob.Y, top + detail * .32, detail * .68, knobD * .84, "#2E3744", finish: SceneFinish.Gloss);
                for (var i = 0; i < 24; i++)
                {
                    var angle = Math.Tau * i / 24;
                    var pivot = new ScenePoint(knob.X + knobD * .408 * Math.Cos(angle),
                        knob.Y + knobD * .408 * Math.Sin(angle), top + detail * .36);
                    m.Rod(pivot, pivot with { Z = top + detail * .94 }, .25, "#37434A");
                }
                m.Box(new(knob.X - .25, knob.Y - knobD * .32, .5, knobD * .32), top + detail, .10,
                    "#E3E7E8", "knob", new(knob.X, -knob.Y, top));
                break;
            case "servo":
                Servo(m, n, top);
                break;
            case "buzzer":
                m.Disc(x, y, z, h - 1, d, "#262D34", sides: 28, finish: SceneFinish.Gloss);
                m.Ring(x, y, top - 1, 1, d, d * .19, "#242B32", SceneFinish.Gloss);
                m.Disc(x, y, top - .88, .045, d * .15, "#181C22", "buzzer");
                m.Text(BoardMeshes.Relative(b, .62, .34, .28, .26), top + .01, "+", "#BFC8CC");
                break;
            case "dht11":
            case "dht22":
                Humidity(m, n, top);
                break;
            case "hc-sr04":
                var ultrasoundFace = m.BevelWithArtwork(b, z, h, color, n.Footprint is { } ultrasound
                    ? dc => ProximitySensorDrawing.Ultrasound(dc, ultrasound) : null);
                foreach (var eye in new[] { "left-eye", "right-eye" })
                {
                    if (!n.Parts.TryGetValue(eye, out var r)) continue;
                    var cx = r.X + r.Width / 2;
                    var cy = r.Y + r.Height / 2;
                    var diameter = Math.Min(r.Width, r.Height);
                    var rim = top + detail * .81;
                    const double recess = 1.05;
                    m.Disc(cx, cy, top, rim - top - recess, diameter, SceneMeshes.Metal, sides: 28);
                    m.Ring(cx, cy, rim - recess, recess, diameter, diameter * .82, SceneMeshes.Metal);
                    m.Disc(cx, cy, rim - recess, .06, diameter * .81, "#354049", sides: 28);
                    if (ultrasoundFace is not null) m.DiscFace(SceneMeshes.Inset(r, diameter * .095, diameter * .095), rim - recess + .06, ultrasoundFace);
                }
                break;
            case "pir":
                m.BevelWithArtwork(b, z, h, color, n.Footprint is { } pir
                    ? dc => ProximitySensorDrawing.Pir(dc, pir) : null);
                PirLens(m, n.Parts.GetValueOrDefault("lens", SceneMeshes.Inset(b, 2, 2)), top, detail);
                break;
            case "relay":
                m.BevelWithArtwork(b, z, h, color, n.Footprint is { } relay
                    ? dc => ActuatorBodyDrawing.Relay(dc, relay) : null);
                m.BeveledBox(SceneMeshes.Inset(b, .19, .19), z + .16, .46, "#173952", bevel: .09);
                break;
            case "battery":
                var cell = n.Parts.GetValueOrDefault("cell", b);
                m.BevelWithArtwork(cell, z, h, "#27313B", dc =>
                    MiscComponentDrawing.BatteryFace(dc, PhysicalDrawing.Rect(cell), n.Value));
                var cap = new LayoutRect(cell.Right - 2.8, cell.Y + .25, 2.8, cell.Height - .5);
                m.BeveledBox(cap, z + .12, h - .16, SceneMeshes.Metal, corner: .37, bevel: .12);
                foreach (var fraction in new[] { .33, .68 })
                {
                    var end = cell.Y + cell.Height * fraction;
                    m.Rod(new(cell.Right - .22, end, z + h * .52), new(cell.Right + 1.1, end, z + h * .52),
                        fraction < .5 ? 2.3 : 3.15, SceneMeshes.Gold, sides: 12);
                }
                break;
            default:
                return false;
        }
        return true;
    }

    private static void Humidity(SceneMeshes m, SceneNode node, double top)
    {
        var b = node.Body;
        const double ventDepth = .88;
        var z = node.Profile.Clearance;
        var face = node.Profile.Material;
        var backing = top - ventDepth - SceneMeshes.RecessBackingClearance;
        m.BeveledBox(b, z, backing - z, face, corner: .72, bevel: .45);
        m.SideWalls(b, backing - .20, top, face);
        for (var row = 0; row < 6; row++)
            for (var col = 0; col < 4; col++)
            {
                var cell = BoardMeshes.Relative(b, .09 + col * .205, .07 + row * .125, .205, .125);
                var hole = SceneMeshes.Inset(cell, cell.Width * .18, cell.Height * .19);
                m.Recess(cell, hole, top, ventDepth, face, "#63828C", "#222C35", chamfer: .13);
            }
        m.Box(BoardMeshes.Relative(b, 0, 0, .09, .82), top - ventDepth, ventDepth, face);
        m.Box(BoardMeshes.Relative(b, .91, 0, .09, .82), top - ventDepth, ventDepth, face);
        m.Box(BoardMeshes.Relative(b, .09, 0, .82, .07), top - ventDepth, ventDepth, face);
        var label = BoardMeshes.Relative(b, 0, .82, 1, .18);
        m.Box(label, top - ventDepth, ventDepth, face);
        m.Face(label, top, new(label, dc => PhysicalDrawing.Text(dc,
            node.Family == "dht22" ? "DHT22" : "DHT11", label.X + 1, label.Y + .35, 1.35,
            PhysicalDrawing.Ink("#263540"))));
    }

    private static void PirLens(SceneMeshes m, LayoutRect lens, double top, double height)
    {
        m.Dome(lens, top - height * .18, height * 1.18, "#E4EBEB", finish: SceneFinish.Gloss);
        const int sections = 24;
        foreach (var fraction in new[] { .34, .59, .80, .92 })
        {
            var z = top + height * (.41 + .59 * Math.Sqrt(1 - fraction * fraction)) + .025;
            var ring = Enumerable.Range(0, sections + 1).Select(i =>
            {
                var angle = Math.Tau * i / sections;
                return new ScenePoint(lens.X + lens.Width * (.5 + fraction * .5 * Math.Cos(angle)),
                    lens.Y + lens.Height * (.5 + fraction * .5 * Math.Sin(angle)), z);
            });
            m.Tube(ring, .088, "#C1CED0");
        }
    }

    private static void Servo(SceneMeshes m, SceneNode n, double top)
    {
        var b = n.Body;
        var z = n.Profile.Clearance;
        var h = n.Profile.Height;
        var d = n.Profile.DetailHeight;
        var casing = n.Parts.GetValueOrDefault("case", b);
        var shaft = n.Parts.GetValueOrDefault("shaft", casing);
        var x = shaft.X + shaft.Width / 2;
        var y = shaft.Y + shaft.Height / 2;
        m.BevelWithArtwork(casing, z, h, n.Profile.Material,
            n.Footprint is { } footprint ? dc => ServoDrawing.Draw(dc, footprint) : null);
        m.BeveledBox(n.Parts.GetValueOrDefault("connector"), z, d * .42, "#242631", corner: .4);
        foreach (var side in new[] { "left-ear", "right-ear" })
        {
            if (!n.Parts.TryGetValue(side, out var ear)) continue;
            m.BeveledBox(ear, z + h * .42, h * .1, "#4387B3", corner: .36);
            m.Ring(ear.X + ear.Width / 2, ear.Y + ear.Height / 2, z + h * .52, .28, 2.1, 1.25, "#315F81", SceneFinish.Matte);
        }
        m.Disc(x, y, top, d * .5, Math.Min(shaft.Width, shaft.Height), "#D7D8D0", finish: SceneFinish.Gloss);
        m.BeveledBox(new(x - 2, y - 1.5, casing.Width * .68, 3), top + d * .5, d * .25, "#F0EAE1",
            corner: .73, bevel: .3, role: "servo", pivot: new(x, -y, top));
    }
}
