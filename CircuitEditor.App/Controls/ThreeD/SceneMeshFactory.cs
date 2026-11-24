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
                m.Tube(node.Path, node.WireRadius * 2, node.Profile.Material, "wire");
                break;
            case SceneLayer.Breadboard:
                BoardMeshes.Breadboard(m, node, cancellationToken);
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
        const double pinSize = .58;
        var plane = node.Profile.Clearance;
        var b = node.Body;
        var axial = node.Profile.Shape == SceneShape.Axial;
        var adjacent = node.Family is "battery" or "servo";
        foreach (var contact in node.Contacts)
        {
            var tip = contact.Position;
            var x = Math.Clamp(tip.X, b.X + Math.Min(.25, b.Width / 2), b.Right - Math.Min(.25, b.Width / 2));
            var y = Math.Clamp(tip.Y, b.Y + Math.Min(.25, b.Height / 2), b.Bottom - Math.Min(.25, b.Height / 2));
            var shoulder = axial ? plane + node.Profile.Height / 2 : Math.Max(plane, .55);
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
                m.Disc(tip.X, tip.Y, tip.Z, .7, 1.05, SceneMeshes.Metal, sides: 6);
                continue;
            }
            var elbow = new ScenePoint(tip.X, tip.Y, shoulder);
            m.Rod(new(x, y, shoulder), elbow, pinSize, SceneMeshes.Metal, sides: 6);
            m.Rod(elbow, tip with { Z = Math.Max(tip.Z, .28) }, pinSize, SceneMeshes.Metal, sides: 6);
        }
    }
}
