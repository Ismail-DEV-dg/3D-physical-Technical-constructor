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
                Led(m, n, x, y, d);
                return true;
            case "photoresistor":
                m.Disc(x, y, z, h, d, "#987345");
                var disc = SceneMeshes.Center(b, d, d);
                m.DiscFace(disc, z + h, new(b, dc => EnvironmentPassiveDrawing.Draw(dc, PhysicalDrawing.Rect(b), false)));
                return true;
            case "thermistor":
                m.Dome(b, z, h, "#425066", finish: SceneFinish.Gloss);
                m.Text(SceneMeshes.Center(b, b.Width * .72, b.Height * .26), z + h, "NTC", "#C9D4DA", role: "mark");
                return true;
            case "npn":
                m.Disc(x, y, z, h, d, color);
                m.BeveledBox(BoardMeshes.Relative(b, .05, .5, .9, .5), z, h, "#252F3A", corner: .25, bevel: .2);
                if (n.Footprint is { } footprint)
                    m.Face(b, z + h, new(b, dc => NpnDrawing.Draw(dc, footprint)));
                return true;
            default:
                return false;
        }
    }

    private static void Led(SceneMeshes m, SceneNode n, double x, double y, double d)
    {
        var z = n.Profile.Clearance;
        var h = n.Profile.Height;
        var b = n.Body;
        var sleeve = n.Family == "led" ? n.Color ?? n.Profile.Material : "#83CED8E1";
        var role = n.Family == "led" ? "lens" : "";
        m.Disc(x, y, z, h * .12, d, "#647179", sides: 20);
        m.Disc(x, y, z + h * .12, h * .45, d * .89, sleeve, role, finish: SceneFinish.Gloss);
        m.Dome(SceneMeshes.Inset(b, b.Width * .055, b.Height * .055),
            z + h * .30, h * .70, sleeve, role, finish: SceneFinish.Gloss);
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
        m.Disc(x, y, z, h, diameter, color, sides: 24, finish: SceneFinish.Gloss);
        m.Disc(x, y, z, h * .09, diameter * 1.035, electrolytic ? "#576674" : "#A46847", sides: 24);
        if (electrolytic)
            m.Disc(x, y, z + h - .14, .14, diameter * .96, SceneMeshes.Metal, sides: 24);
        if (n.Footprint is { } footprint)
            m.DiscFace(SceneMeshes.Center(part, diameter, diameter), z + h, new(part,
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
        Sleeve(0, 1, d * .92, n.Profile.Material, n.Family == "diode" ? SceneFinish.Gloss : SceneFinish.Matte);
        if (n.Family == "axial-resistor")
        {
            Sleeve(.03, .14, d, n.Profile.Material);
            Sleeve(.86, .97, d, n.Profile.Material);
            var bands = ResistorMarking.GetBands(n.Value, n.Tolerance);
            for (var i = 0; i < bands.Count; i++)
            {
                var start = .19 + i * (.66 / bands.Count);
                Sleeve(start, start + .067, d * .944, bands[i]);
            }
        }
        if (n.Family == "diode") Sleeve(.76, .86, d * .951, "#ACBCC7", SceneFinish.Metal);
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
