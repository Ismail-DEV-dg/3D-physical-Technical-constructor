using System.Collections.Immutable;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.App.Controls.ThreeD;

internal sealed class SceneMeshFactory : ISceneMeshFactory
{
    public ImmutableArray<SceneMeshPart> Build(SceneNode node, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SceneMeshes m = new();
        var b = node.Body;
        var z = node.Profile.Clearance;
        var h = node.Profile.Height;
        switch (node.Layer)
        {
            case SceneLayer.Wire:
                if (node.Path.Length >= 2)
                {
                    var start = WireCollar(node.Path[0], node.Path[1]);
                    var end = WireCollar(node.Path[^1], node.Path[^2]);
                    m.Tube(node.Path.Select((p, index) => index == 0 ? start : index == node.Path.Length - 1 ? end : p),
                        node.WireRadius * 2, node.Profile.Material, "wire");
                    m.Rod(node.Path[0], start, node.WireRadius * 1.20, SceneMeshes.Gold, "wire-tip", sides: 8);
                    m.Rod(node.Path[^1], end, node.WireRadius * 1.20, SceneMeshes.Gold, "wire-tip", sides: 8);
                }
                break;
            case SceneLayer.Breadboard:
                BoardMeshes.Breadboard(m, node, cancellationToken);
                break;
            case SceneLayer.Component when node.Family == CircuitEditor.Core.Physical.SplitterGeometry.ComponentId:
                SplitterMeshes.Draw(m, node);
                break;
            default:
                if (!BoardMeshes.Draw(m, node) && !DisplayMeshes.Draw(m, node) &&
                    !PassiveMeshes.Draw(m, node) && !DeviceMeshes.Draw(m, node))
                    m.BeveledBox(b, z, h, node.Profile.Material);
                Leads(m, node);
                break;
        }
        cancellationToken.ThrowIfCancellationRequested();
        return m.Finish();
    }

    private static void Leads(SceneMeshes m, SceneNode node)
    {
        const double pinSize = .54;
        var plane = node.Profile.Clearance;
        var b = node.Body;
        var axial = node.Profile.Shape == SceneShape.Axial;
        var adjacent = node.Family is "battery" or "servo";
        foreach (var contact in node.Contacts)
        {
            var tip = contact.Position;
            if (node.Family == "led")
            {
                const double leadWidth = .46;
                var shoulderHeight = plane + .16 - tip.Z;
                m.Box(new(tip.X - leadWidth / 2, tip.Y - leadWidth / 2, leadWidth, leadWidth),
                    tip.Z, shoulderHeight, SceneMeshes.Metal, "lead");
                continue;
            }
            if (axial)
            {
                AxialLead(m, node, tip);
                continue;
            }
            if (node.Family == "arduino-uno") continue; // Female sockets are modelled by ControllerMeshes.
            if (node.Family is "arduino-nano" or "esp32-devkit")
            {
                var bottom = contact.HoleId is null ? tip with { Z = .05 } : tip;
                var post = tip with { Z = plane + node.Profile.Height + SceneContactDepth.HeaderPost };
                m.Rod(bottom, post, .66, SceneMeshes.Gold, "lead", sides: 4);
                continue;
            }
            var x = Math.Clamp(tip.X, b.X + Math.Min(.25, b.Width / 2), b.Right - Math.Min(.25, b.Width / 2));
            var y = Math.Clamp(tip.Y, b.Y + Math.Min(.25, b.Height / 2), b.Bottom - Math.Min(.25, b.Height / 2));
            var shoulder = Math.Max(plane, .55);
            if (node.Family == "rgb-led")
            {
                var lens = PassiveMeshes.LedLens(node);
                x = Math.Clamp(tip.X, lens.X + lens.Width * .24, lens.Right - lens.Width * .24);
                y = lens.Y + lens.Height * .64;
                shoulder = plane + .22;
            }
            if (node.Family == "capacitor")
            {
                var part = node.Parts.GetValueOrDefault(node.Variant == "Electrolytic" ? "electrolytic" : "ceramic", b);
                y = part.Y + part.Height * .62;
                x = Math.Clamp(tip.X, part.X + part.Width * .27, part.Right - part.Width * .27);
                shoulder = plane + .2;
            }
            if (adjacent)
            {
                var source = node.Parts.GetValueOrDefault(node.Family == "servo" ? "case" : "cell", b);
                var start = new ScenePoint(source.Right - .5, Math.Clamp(tip.Y, source.Y + 1, source.Bottom - 1),
                    shoulder + node.Profile.Height / 4);
                var wireColor = contact.Id switch
                {
                    "1" or "power" or "positive" => "#DF483D", "2" or "ground" or "negative" => "#434753", _ => "#CE9138"
                };
                m.Tube([start, new((start.X + tip.X) / 2, (start.Y + tip.Y) / 2, shoulder + 1), tip], .85, wireColor);
                m.Disc(tip.X, tip.Y, tip.Z, .7, .68, SceneMeshes.Metal, "lead", sides: 8);
                continue;
            }
            var elbow = new ScenePoint(tip.X, tip.Y, shoulder);
            var rectangular = node.Family is "led" or "rgb-led" or "dip-chip" or "button" or "switch";
            var sides = rectangular ? 4 : 10;
            m.Rod(new(x, y, shoulder), elbow, pinSize, SceneMeshes.Metal, "lead", sides: sides);
            m.Rod(elbow, tip, pinSize, SceneMeshes.Metal, "lead", sides: sides);
        }
    }

    private static void AxialLead(SceneMeshes m, SceneNode node, ScenePoint tip)
    {
        const double diameter = .52;
        const string Steel = "#ACB0B4";
        var body = node.Body;
        var middleX = body.X + body.Width / 2;
        var y = body.Y + body.Height / 2;
        var z = node.Profile.Clearance + node.Profile.Height / 2;
        var start = new ScenePoint(tip.X < middleX ? body.X + .38 : body.Right - .38, y, z);
        var dx = tip.X - start.X;
        var dy = tip.Y - start.Y;
        var span = Math.Sqrt(dx * dx + dy * dy);
        if (span < .001 || z <= tip.Z)
        {
            m.Rod(start, tip, diameter, Steel, "lead", sides: 12, finish: SceneFinish.Metal);
            return;
        }
        var bend = Math.Min(.88, Math.Min(span / 2, (z - tip.Z) / 2));
        var elbow = new ScenePoint(tip.X, tip.Y, z);
        var a = elbow with { X = elbow.X - dx * bend / span, Y = elbow.Y - dy * bend / span };
        var b = elbow with { Z = z - bend };
        const int BendSegments = 8;
        var points = new List<ScenePoint> { start };
        for (var i = 0; i <= BendSegments; i++)
        {
            var angle = Math.PI / 2 * i / BendSegments;
            points.Add(new(a.X + dx / span * bend * Math.Sin(angle), a.Y + dy / span * bend * Math.Sin(angle),
                b.Z + bend * Math.Cos(angle)));
        }
        points.Add(tip);
        m.Tube(points, diameter, Steel, "lead", sides: 12, finish: SceneFinish.Metal);
    }

    private static ScenePoint WireCollar(ScenePoint tip, ScenePoint inner)
    {
        var distance = Math.Sqrt(Math.Pow(tip.X - inner.X, 2) + Math.Pow(tip.Y - inner.Y, 2) + Math.Pow(tip.Z - inner.Z, 2));
        if (distance <= 0) return tip;
        var length = Math.Min(.72 / distance, .42);
        return new ScenePoint(tip.X + (inner.X - tip.X) * length, tip.Y + (inner.Y - tip.Y) * length,
            tip.Z + (inner.Z - tip.Z) * length);
    }
}
