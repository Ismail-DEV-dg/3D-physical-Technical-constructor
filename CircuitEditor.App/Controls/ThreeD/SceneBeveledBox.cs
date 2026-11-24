using System.Numerics;
using CircuitEditor.Core.Layout;
using HelixToolkit.Geometry;

namespace CircuitEditor.App.Controls.ThreeD;

internal static class SceneBeveledBox
{
    public static void Add(MeshBuilder mesh, LayoutRect footprint, double z, double height, double corner, double bevel)
    {
        var limit = Math.Min(footprint.Width, footprint.Height);
        corner = Math.Min(corner, limit * .24);
        bevel = Math.Min(bevel, Math.Min(height * .32, limit * .12));
        var lower = Ring(footprint, z, corner);
        var shoulder = Ring(footprint, z + height - bevel, corner);
        var inset = SceneMeshes.Inset(footprint, bevel, bevel);
        var upper = Ring(inset, z + height, Math.Max(corner - bevel / 2, corner / 2));
        var middleX = (float)(footprint.X + footprint.Width / 2);
        var middleY = (float)-(footprint.Y + footprint.Height / 2);
        var bottom = new Vector3(middleX, middleY, (float)z);
        var top = new Vector3(middleX, middleY, (float)(z + height));
        for (var i = 0; i < lower.Length; i++)
        {
            var next = (i + 1) % lower.Length;
            mesh.AddQuad(lower[next], lower[i], shoulder[i], shoulder[next]);
            mesh.AddQuad(shoulder[next], shoulder[i], upper[i], upper[next]);
            mesh.AddTriangle(top, upper[next], upper[i]);
            mesh.AddTriangle(bottom, lower[i], lower[next]);
        }
    }

    private static Vector3[] Ring(LayoutRect b, double z, double corner)
    {
        return
        [
            At(b.X + corner, b.Y), At(b.Right - corner, b.Y),
            At(b.Right, b.Y + corner), At(b.Right, b.Bottom - corner),
            At(b.Right - corner, b.Bottom), At(b.X + corner, b.Bottom),
            At(b.X, b.Bottom - corner), At(b.X, b.Y + corner)
        ];
        Vector3 At(double x, double y) => new((float)x, (float)-y, (float)z);
    }
}
