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
                m.BeveledBox(b, z, h, SceneMeshes.Metal, corner: .5, bevel: .22);
                var slot = n.Parts.GetValueOrDefault("slot", SceneMeshes.Center(b, b.Width * .75, b.Height * .5));
                m.BeveledBox(slot, top, .18, "#161B25", corner: .26, bevel: .04);
                m.BeveledBox(SceneMeshes.Center(slot, slot.Width * .34, slot.Height * .85), top + .18, detail,
                    "#536273", corner: .31, bevel: .22, role: "switch", pivot: new(slot.Width * .27, 0, 0));
                break;
            case "potentiometer":
                var knob = n.Knob ?? new(x, y);
                var knobD = n.KnobRadius > 0 ? n.KnobRadius * 2 : d * .72;
                m.BeveledBox(b, z, h, "#33685F", corner: .8);
                m.Text(new(b.X, b.Y, b.Width, 1.9), top, n.Value, "#E7EEE5", role: "mark");
                m.Disc(knob.X, knob.Y, top, detail * .32, knobD, SceneMeshes.Metal);
                m.Disc(knob.X, knob.Y, top + detail * .32, detail * .68, knobD * .84, "#2E3744", finish: SceneFinish.Gloss);
                m.Box(new(knob.X - .25, knob.Y - knobD * .32, .5, knobD * .32), top + detail, .10,
                    "#E3E7E8", "knob", new(knob.X, -knob.Y, top));
                break;
            case "servo":
                Servo(m, n, top);
                break;
            case "buzzer":
                m.Disc(x, y, z, h, d, "#212A33", finish: SceneFinish.Gloss);
                if (n.Footprint is { } buzzer)
                    m.DiscFace(b, top, new(b, dc => ActuatorBodyDrawing.Buzzer(dc, buzzer)));
                m.Disc(x, y, top + .03, .045, d * .13, "#171E27", "buzzer");
                break;
            case "dht11":
            case "dht22":
                m.BeveledBox(b, z, h, color, corner: .72, bevel: .52);
                if (n.Footprint is { } dht)
                    m.Face(b, top, new(b, dc => HumiditySensorDrawing.Draw(dc, dht)));
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
                    m.Disc(cx, cy, top, rim - top, diameter, SceneMeshes.Metal, sides: 24);
                    if (ultrasoundFace is not null) m.DiscFace(r, rim, ultrasoundFace);
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
                break;
            case "battery":
                var cell = n.Parts.GetValueOrDefault("cell", b);
                m.BevelWithArtwork(cell, z, h, "#27313B", dc =>
                    MiscComponentDrawing.BatteryFace(dc, PhysicalDrawing.Rect(cell), n.Value));
                var cap = new LayoutRect(cell.Right - 2.8, cell.Y + .25, 2.8, cell.Height - .5);
                m.BeveledBox(cap, z + .12, h - .16, SceneMeshes.Metal, corner: .37, bevel: .12);
                break;
            default:
                return false;
        }
        return true;
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
            m.Disc(ear.X + ear.Width / 2, ear.Y + ear.Height / 2, z + h * .52, .07, 1.5, "#2F5167");
        }
        m.Disc(x, y, top, d * .5, Math.Min(shaft.Width, shaft.Height), "#D7D8D0", finish: SceneFinish.Gloss);
        m.BeveledBox(new(x - 2, y - 1.5, casing.Width * .68, 3), top + d * .5, d * .25, "#F0EAE1",
            corner: .73, bevel: .3, role: "servo", pivot: new(x, -y, top));
    }
}
