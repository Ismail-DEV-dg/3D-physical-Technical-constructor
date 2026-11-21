using CircuitEditor.Core.Layout;
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
                if (n.Variant == "Ceramic")
                {
                    var ceramic = n.Parts.GetValueOrDefault("ceramic", b);
                    var center = ceramic.X + ceramic.Width / 2;
                    m.Rod(new(center, ceramic.Y, z + h / 2), new(center, ceramic.Bottom, z + h / 2), h, "#B86D3E");
                }
                else
                {
                    var cylinder = n.Parts.GetValueOrDefault("electrolytic", b);
                    var r = Math.Min(cylinder.Width, cylinder.Height);
                    m.Disc(x, y, z, h, r, color);
                    m.Disc(x, y, z + h, .25, r * .93, SceneMeshes.Metal);
                    m.Box(new(x + r * .26, y - r * .26, r * .13, r * .52), z + h, .3, "#D0D9D8");
                }
                return true;
            case "led":
            case "rgb-led":
                m.Disc(x, y, z, h * .18, d, "#56616C");
                if (n.Family == "led") m.Dome(b, z + h * .12, h * .88, n.Color ?? color, "lens");
                else
                {
                    m.Dome(b, z + h * .12, h * .88, "#83D2E1F0");
                    const double channelWidth = .22;
                    m.Dome(BoardMeshes.Relative(b, .13, .32, channelWidth, .36), z + h * .24, h * .44, "#873F40", "rgb-r");
                    m.Dome(BoardMeshes.Relative(b, .39, .32, channelWidth, .36), z + h * .24, h * .44, "#396B42", "rgb-g");
                    m.Dome(BoardMeshes.Relative(b, .65, .32, channelWidth, .36), z + h * .24, h * .44, "#375B81", "rgb-b");
                }
                return true;
            case "photoresistor":
                m.Disc(x, y, z, h, d, color);
                m.Tube(Enumerable.Range(0, 10).Select(i =>
                    new ScenePoint(x + (i - 4.5) * d * .066, y + (i % 2 == 0 ? .22 : -.22) * d, z + h)),
                    .22, "#864630");
                return true;
            case "thermistor":
                m.Dome(b, z, h, color);
                return true;
            case "npn":
                m.Disc(x, y, z, h, d, color);
                m.Box(BoardMeshes.Relative(b, .13, .1, .74, .36), z, h, color);
                m.Text(b, z + h, "NPN", "#CFDBDD", role: "mark");
                return true;
            default:
                return false;
        }
    }

    private static void Axial(SceneMeshes m, SceneNode n)
    {
        var b = n.Body;
        var plane = n.Profile.Clearance + n.Profile.Height / 2;
        var y = b.Y + b.Height / 2;
        var d = Math.Min(n.Profile.Height, b.Height);
        double X(double ratio) => b.X + b.Width * ratio;
        void Sleeve(double start, double end, double diameter, string color) =>
            m.Rod(new(X(start), y, plane), new(X(end), y, plane), diameter, color);
        Sleeve(0, 1, d, n.Profile.Material);
        if (n.Family == "axial-resistor")
        {
            var bands = ResistorMarking.GetBands(n.Value, n.Tolerance);
            for (var i = 0; i < bands.Count; i++)
            {
                var start = .15 + i * (.72 / bands.Count);
                Sleeve(start, start + .072, d * 1.045, bands[i]);
            }
        }
        if (n.Family == "diode") Sleeve(.74, .84, d * 1.04, "#BEC5CE");
        if (n.Family == "inductor")
        {
            const int turns = 6;
            const int pointsPerTurn = 10;
            m.Tube(Enumerable.Range(0, turns * pointsPerTurn + 1).Select(i =>
            {
                var phase = i * Math.Tau / pointsPerTurn;
                return new ScenePoint(X(.12 + .76 * i / (turns * pointsPerTurn)), y + d / 2 * Math.Sin(phase),
                    plane + d / 2 * Math.Cos(phase));
            }), .3, "#CE863F");
        }
    }
}
