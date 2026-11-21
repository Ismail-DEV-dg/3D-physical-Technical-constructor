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
        var t = z + h;
        var detail = n.Profile.DetailHeight;
        var color = n.Profile.Material;
        var x = b.X + b.Width / 2;
        var y = b.Y + b.Height / 2;
        var d = Math.Min(b.Width, b.Height);
        switch (n.Family)
        {
            case "button":
                m.Box(b, z, h, color);
                m.Disc(x, y, t - .35, detail + .35, d * .66, SceneMeshes.Dark, "button");
                break;
            case "switch":
                m.Box(b, z, h, color);
                var slot = n.Parts.GetValueOrDefault("slot", SceneMeshes.Center(b, b.Width * .75, b.Height * .5));
                m.Box(slot, t, .18, "#161B25");
                m.Box(SceneMeshes.Center(slot, slot.Width * .34, slot.Height * .85), t + .18, detail,
                    "#A2A8AE", "switch", new(slot.Width * .27, 0, 0));
                break;
            case "potentiometer":
                var knob = n.Knob ?? new(x, y);
                var knobD = n.KnobRadius > 0 ? n.KnobRadius * 2 : d * .72;
                m.Disc(knob.X, knob.Y, z, h, d, color);
                m.Disc(knob.X, knob.Y, t, detail, knobD, "#20232C");
                m.Box(new(knob.X + .1, knob.Y - .25, knobD * .42, .5), t + detail, .12,
                    "#E3E7E8", "knob", new(knob.X, -knob.Y, t));
                break;
            case "servo":
                Servo(m, n, t);
                break;
            case "buzzer":
                m.Disc(x, y, z, h, d, color, "buzzer");
                m.Disc(x, y, t, detail * .15, d * .20, "#131520");
                m.Text(BoardMeshes.Relative(b, .55, .18, .21, .18), t, "+", "#D5DFE2", role: "mark");
                break;
            case "dht11":
            case "dht22":
                m.Box(b, z, h, color);
                for (var row = 0; row < 3; row++)
                    for (var column = 0; column < 5; column++)
                        m.Box(BoardMeshes.Relative(b, .13 + column * .15, .15 + row * .24, .08, .13),
                            t, detail * .12, "#444F5F");
                break;
            case "hc-sr04":
                m.Box(b, z, h, color);
                foreach (var eye in new[] { "left-eye", "right-eye" })
                {
                    var r = n.Parts.GetValueOrDefault(eye, b);
                    var cx = r.X + r.Width / 2;
                    var cy = r.Y + r.Height / 2;
                    var diameter = Math.Min(r.Width, r.Height);
                    m.Disc(cx, cy, t, detail * .84, diameter, SceneMeshes.Metal);
                    m.Disc(cx, cy, t + detail * .84, detail * .045, diameter * .73, "#414753");
                }
                break;
            case "pir":
                m.Box(b, z, h, color);
                m.Dome(n.Parts.GetValueOrDefault("lens", SceneMeshes.Inset(b, 2, 2)), t, detail, "#EDF3F5");
                break;
            case "relay":
                m.Box(b, z, h, color);
                m.Text(b, t, "RELAY", "#DBE9F6", role: "mark");
                break;
            case "battery":
                var cell = n.Parts.GetValueOrDefault("cell", b);
                m.Box(cell, z, h, color);
                m.Box(BoardMeshes.Relative(cell, .1, .04, .8, .25), t, detail * .42, "#BB8051");
                m.Text(cell, t + detail * .42, "+  " + n.Value + "  −", "#EFE0D9", role: "mark");
                break;
            default:
                return false;
        }
        return true;
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
        m.Box(casing, z, h, n.Profile.Material);
        foreach (var side in new[] { "left-ear", "right-ear" })
            m.Box(n.Parts.GetValueOrDefault(side), z + h * .42, h * .1, "#4387B3");
        m.Disc(x, y, top, d * .5, Math.Min(shaft.Width, shaft.Height), "#DBDCD7");
        m.Box(new(x - 2, y - 1.5, casing.Width * .68, 3), top + d * .5, d * .25,
            "#F0EAE1", "servo", new(x, -y, top));
    }
}
