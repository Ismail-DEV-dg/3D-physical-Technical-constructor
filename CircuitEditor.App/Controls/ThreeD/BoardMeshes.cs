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
        const double trenchDepth = .92;
        const double opening = 1.24;
        var floor = height - SceneContactDepth.BreadboardBore;
        const double baseHeight = .78;
        m.BeveledBox(b, 0, baseHeight, "#C0C2BC", corner: .9, bevel: .14);
        m.Box(b, baseHeight, floor - baseHeight - .10, "#DBDAD2");
        // The shell owns the outside walls; the strips only fill the top between cavities.
        m.SideWalls(b, floor - .10, height, "#DBDAD2");
        if (node.Breadboard is not { } board) return;

        var pitch = board.HolePitch;
        var terminalColumns = board.Holes.Where(hole => hole.GroupId.Contains("/row/", StringComparison.Ordinal))
            .Select(hole => hole.Position.X).Distinct().Order().ToArray();
        var center = terminalColumns.Zip(terminalColumns.Skip(1), (a, c) => (Left: a + pitch / 2, Right: c - pitch / 2))
            .OrderByDescending(gap => gap.Right - gap.Left).FirstOrDefault();
        var columns = node.Contacts.GroupBy(pin => pin.Position.X).OrderBy(group => group.Key).ToArray();
        var left = b.X;
        foreach (var column in columns)
        {
            token.ThrowIfCancellationRequested();
            var x = column.Key;
            var start = x - pitch / 2;
            if (start - left > .0001)
            {
                var channel = Math.Abs(left - center.Left) < .001 && Math.Abs(start - center.Right) < .001;
                var strip = new LayoutRect(left, b.Y, start - left, b.Height);
                if (channel)
                {
                    var slot = SceneMeshes.Inset(strip, .27, 2.1);
                    m.Recess(strip, slot, height, trenchDepth, node.Profile.Material, "#C4C5BA", "#B3B7AE", chamfer: .12);
                }
                else m.TopFace(strip, height, node.Profile.Material);
            }
            var edge = b.Y;
            foreach (var pin in column.OrderBy(pin => pin.Position.Y))
            {
                var y = pin.Position.Y;
                var low = y - pitch / 2;
                if (low - edge > .0001) m.TopFace(new(start, edge, pitch, low - edge), height, node.Profile.Material);
                m.Recess(new(start, low, pitch, pitch), new(x - opening / 2, y - opening / 2, opening, opening),
                    height, SceneContactDepth.BreadboardBore, node.Profile.Material, "#AEB6B3", "#333A40");
                edge = y + pitch / 2;
            }
            if (edge < b.Bottom) m.TopFace(new(start, edge, pitch, b.Bottom - edge), height, node.Profile.Material);
            left = start + pitch;
        }
        if (left < b.Right) m.TopFace(new(left, b.Y, b.Right - left, b.Height), height, node.Profile.Material);
        m.Face(b, height, new(b, dc => BreadboardDrawing.DrawBase(dc, board, false, markingsOnly: true)));
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
            var key = new LayoutPoint(b.X + b.Width * .19, b.Y + b.Height * .22);
            m.Disc(key.X, key.Y, z + h + .035, .05, .8, "#626E77", sides: 16);
            return true;
        }
        return ControllerMeshes.Draw(m, node);
    }

    public static LayoutRect Relative(LayoutRect r, double x, double y, double w, double h) =>
        new(r.X + r.Width * x, r.Y + r.Height * y, r.Width * w, r.Height * h);
}
