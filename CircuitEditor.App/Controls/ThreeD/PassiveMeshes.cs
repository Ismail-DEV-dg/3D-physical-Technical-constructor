using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.App.Controls.ThreeD;

internal static class PassiveMeshes
{
    public static bool Draw(SceneMeshes m, SceneNode n)
    {
        var b = n.Body;
        var z = n.Profile.Clearance;
        var h = n.Profile.Height;
        var color = n.Profile.Material;
        var x = b.X + b.Width / 2;
        var y = b.Y + b.Height / 2;
        var d = Math.Min(b.Width, b.Height);
        switch (n.Family)
        {
            case "axial-resistor":
            case "diode":
            case "inductor":
                Axial(m, n);
                return true;
            case "capacitor":
                Capacitor(m, n);
                return true;
            case "led":
            case "rgb-led":
                Led(m, n);
                return true;
            case "photoresistor":
                m.Disc(x, y, z, h * .72, d, "#A58150");
                m.Ring(x, y, z + h * .68, h * .32, d, d * .8, "#B49A66");
                var disc = SceneMeshes.Center(b, d, d);
                m.DiscFace(SceneMeshes.Inset(disc, d * .1, d * .1), z + h * .78,
                    new(b, dc => EnvironmentPassiveDrawing.Draw(dc, PhysicalDrawing.Rect(b), false)));
                return true;
            case "thermistor":
                m.Lens(x, y, z, d * .83, h, "#2C3840", "");
                m.Text(SceneMeshes.Center(b, b.Width * .72, b.Height * .26), z + h, "NTC", "#C9D4DA", role: "mark");
                return true;
            case "npn":
                m.Disc(x, y - .35, z, h - .12, d * .94, color);
                m.BeveledBox(BoardMeshes.Relative(b, .05, .36, .9, .55), z, h, "#252F3A", corner: .22, bevel: .18);
                if (n.Footprint is { } footprint)
                    m.Face(b, z + h, new(b, dc => NpnDrawing.Draw(dc, footprint)));
                return true;
            default:
                return false;
        }
    }

    internal static LayoutRect LedLens(SceneNode n)
    {
        var b = n.Body;
        var diameter = Math.Min(b.Width, b.Height);
        return n.Parts.GetValueOrDefault("lens", new(b.X + (b.Width - diameter) / 2, b.Y, diameter, diameter));
    }

    private static void Led(SceneMeshes m, SceneNode n)
    {
        var z = n.Profile.Clearance;
        var h = n.Profile.Height;
        var b = LedLens(n);
        var d = Math.Min(b.Width, b.Height);
        var x = b.X + b.Width / 2;
        var y = b.Y + b.Height / 2;
        var sleeve = n.Family == "led" ? n.Color ?? n.Profile.Material : "#68BFC9D3";
        var role = n.Family == "led" ? "lens" : "";
        var collar = h * .11;
        m.Disc(x, y, z, collar, d, sleeve, role, sides: 36, finish: SceneFinish.Gloss);
        const double inset = .015;
        m.Lens(x, y, z + collar - inset, d * .865, h - collar + inset, sleeve, role);
        if (n.Family != "rgb-led") return;

        const double channelWidth = .22;
        m.Dome(BoardMeshes.Relative(b, .13, .32, channelWidth, .36), z + h * .23, h * .31, "#873F40", "rgb-r", SceneFinish.Gloss);
        m.Dome(BoardMeshes.Relative(b, .39, .32, channelWidth, .36), z + h * .23, h * .31, "#396B42", "rgb-g", SceneFinish.Gloss);
        m.Dome(BoardMeshes.Relative(b, .65, .32, channelWidth, .36), z + h * .23, h * .31, "#375B81", "rgb-b", SceneFinish.Gloss);
    }

    private static void Capacitor(SceneMeshes m, SceneNode n)
    {
        var electrolytic = n.Variant == nameof(CapacitorKind.Electrolytic);
        var part = n.Parts.GetValueOrDefault(electrolytic ? "electrolytic" : "ceramic", n.Body);
        var z = n.Profile.Clearance;
        var h = n.Profile.Height * (electrolytic ? 1 : .50);
        var diameter = Math.Min(part.Width, part.Height);
        var x = part.X + part.Width / 2;
        var y = part.Y + part.Height / 2;
        var color = electrolytic ? "#263548" : "#AA5E35";
        if (!electrolytic)
        {
            m.Lens(x, y, z, diameter * .96, h, color, "");
            m.DiscFace(SceneMeshes.Center(part, diameter * .40, diameter * .40), z + h - .11,
                new(part, dc => PassiveBodyDrawing.Capacitor(dc, footprint: n.Footprint!, kind: CapacitorKind.Ceramic, value: n.Value)));
            return;
        }
        m.Disc(x, y, z, h - .36, diameter, color, sides: 28, finish: SceneFinish.Gloss);
        m.Disc(x, y, z, h * .09, diameter * 1.035, "#576674", sides: 28);
        m.Ring(x, y, z + h - .36, .36, diameter, diameter * .85, SceneMeshes.Metal);
        if (electrolytic)
            m.Disc(x, y, z + h - .36, .25, diameter * .85, SceneMeshes.Metal, sides: 28);
        if (n.Footprint is { } footprint)
            m.DiscFace(SceneMeshes.Center(part, diameter * .85, diameter * .85), z + h - .10, new(part,
                dc => PassiveBodyDrawing.Capacitor(dc, footprint, electrolytic ? CapacitorKind.Electrolytic : CapacitorKind.Ceramic, n.Value)));
    }

    private static void Axial(SceneMeshes m, SceneNode n)
    {
        var b = n.Body;
        var plane = n.Profile.Clearance + n.Profile.Height / 2;
        var y = b.Y + b.Height / 2;
        var d = Math.Min(n.Profile.Height, b.Height);
        double X(double ratio) => b.X + b.Width * ratio;
        void Sleeve(double start, double end, double diameter, string color, SceneFinish finish = SceneFinish.Matte) =>
            m.Rod(new(X(start), y, plane), new(X(end), y, plane), diameter, color, finish: finish);
        if (n.Family == "axial-resistor")
        {
            void Taper(double start, double end, double from, double to) =>
                m.Taper(new(X(start), y, plane), new(X(end), y, plane), d * from, d * to, n.Profile.Material);
            Taper(0, .085, .68, 1);
            Sleeve(.085, .145, d, n.Profile.Material);
            Taper(.145, .20, 1, .85);
            Sleeve(.20, .80, d * .85, n.Profile.Material);
            Taper(.80, .855, .85, 1);
            Sleeve(.855, .915, d, n.Profile.Material);
            Taper(.915, 1, 1, .68);
            var bands = ResistorMarking.GetBands(n.Value, n.Tolerance);
            for (var i = 0; i < bands.Count; i++)
            {
                var start = .21 + i * (.57 / bands.Count);
                Sleeve(start, start + .059, d * .865, bands[i]);
            }
            return;
        }
        Sleeve(.035, .965, d * .92, n.Profile.Material, n.Family == "diode" ? SceneFinish.Gloss : SceneFinish.Matte);
        Sleeve(0, .035, d * .74, "#575D65");
        Sleeve(.965, 1, d * .74, "#575D65");
        if (n.Family == "diode") Sleeve(.76, .86, d * .95, "#ACBCC7", SceneFinish.Metal);
        if (n.Family == "inductor")
        {
            const int turns = 12;
            const int pointsPerTurn = 8;
            m.Tube(Enumerable.Range(0, turns * pointsPerTurn + 1).Select(i =>
            {
                var phase = i * Math.Tau / pointsPerTurn;
                return new ScenePoint(X(.07 + .86 * i / (turns * pointsPerTurn)), y + d * .47 * Math.Sin(phase),
                    plane + d * .47 * Math.Cos(phase));
            }), .2, "#BD7540");
            m.Text(SceneMeshes.Center(b, b.Width * .72, b.Height * .38), plane + d * .51,
                n.Value, "#24221A", "#E2D1AB", "mark");
        }
    }
}
