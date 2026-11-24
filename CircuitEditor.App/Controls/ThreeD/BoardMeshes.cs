using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.App.Controls.ThreeD;

internal static class BoardMeshes
{
    public static void Breadboard(SceneMeshes m, SceneNode node, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var b = node.Body;
        var height = node.Profile.Height;
        m.BeveledBox(b, 0, height * .76, "#B4BDC7", corner: 1.45, bevel: .6);
        m.BeveledBox(SceneMeshes.Inset(b, .46, .46), height * .72, height * .28,
            node.Profile.Material, corner: 1.15, bevel: .22);
        if (node.Breadboard is { } board)
            m.Face(b, height, new(b, dc => BreadboardDrawing.DrawBase(dc, board, false)));
    }

    public static bool Draw(SceneMeshes m, SceneNode node)
    {
        if (node.Family == "dip-chip")
        {
            var b = node.Body;
            var z = node.Profile.Clearance;
            var h = node.Profile.Height;
            m.BeveledBox(b, z, h, node.Profile.Material, bevel: .4);
            if (node.Footprint is { } footprint)
                m.Face(b, z + h, new(b, dc => ChipPackageDrawing.Draw(dc, footprint, node.Name)));
            return true;
        }
        return ControllerMeshes.Draw(m, node);
    }

    public static LayoutRect Relative(LayoutRect r, double x, double y, double w, double h) =>
        new(r.X + r.Width * x, r.Y + r.Height * y, r.Width * w, r.Height * h);
}
