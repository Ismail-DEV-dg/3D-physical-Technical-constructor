using System.Collections.Immutable;
using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Scene;
using MeshBuilder = HelixToolkit.Geometry.MeshBuilder;

namespace CircuitEditor.App.Controls.ThreeD;

internal sealed class SceneMeshes
{
    internal const string Metal = "#A3ADB4";
    internal const string Dark = "#242936";
    internal const string Gold = "#CBA561";
    internal const string Label = "label";
    private const int RoundSides = 16;
    private const double Detail = .08;
    private readonly Dictionary<(string Color, string Role, Point3D Pivot), MeshBuilder> meshes = [];
    private readonly List<SceneMeshPart> textures = [];

    public static Point3D Point(ScenePoint p) => new(p.X, -p.Y, p.Z);
    private static Vector3 Vector(double x, double y, double z) => new((float)x, (float)-y, (float)z);

    public void Box(LayoutRect r, double z, double h, string color, string role = "", Point3D pivot = default)
    {
        if (r.Width <= 0 || r.Height <= 0 || h <= 0) return;
        Mesh(color, role, pivot).AddBox(Vector(r.X + r.Width / 2, r.Y + r.Height / 2, z + h / 2),
            (float)r.Width, (float)r.Height, (float)h);
    }

    public void Disc(double x, double y, double z, double h, double d, string color, string role = "",
        Point3D pivot = default, int sides = RoundSides) =>
        Rod(new(x, y, z), new(x, y, z + h), d, color, role, pivot, sides);

    public void Rod(ScenePoint a, ScenePoint b, double d, string color, string role = "",
        Point3D pivot = default, int sides = RoundSides)
    {
        var from = Vector(a.X, a.Y, a.Z);
        var to = Vector(b.X, b.Y, b.Z);
        if (from != to)
            Mesh(color, role, pivot).AddCylinder(from, to, (float)d, sides, true, true);
    }

    public void Dome(LayoutRect r, double z, double height, string color, string role = "") =>
        Mesh(color, role, default).AddEllipsoid(Vector(r.X + r.Width / 2, r.Y + r.Height / 2, z + height / 2),
            (float)(r.Width / 2), (float)(r.Height / 2), (float)(height / 2), RoundSides, 8);

    public void Tube(IEnumerable<ScenePoint> path, double diameter, string color, string role = "")
    {
        List<Vector3> points = [];
        foreach (var p in path)
        {
            var point = Vector(p.X, p.Y, p.Z);
            if (points.Count == 0 || point != points[^1]) points.Add(point);
        }
        if (points.Count >= 2) Mesh(color, role, default).AddTube(points, (float)diameter, 6, false, true, true);
    }

    public void Text(LayoutRect area, double z, string text, string foreground, string background = "Transparent",
        string role = Label)
    {
        if (string.IsNullOrWhiteSpace(text) && role == Label || area.Width <= 0 || area.Height <= 0) return;
        Point3D[] points = [new(area.X, -area.Y, z + Detail), new(area.X, -area.Bottom, z + Detail),
            new(area.Right, -area.Bottom, z + Detail), new(area.Right, -area.Y, z + Detail)];
        MeshGeometry3D plane = new() { Positions = new(points), TriangleIndices = new([0, 1, 2, 0, 2, 3]),
            TextureCoordinates = new([new(0, 0), new(0, 1), new(1, 1), new(1, 0)]) };
        plane.Freeze();
        var key = FormattableString.Invariant($"text:{area.Width / area.Height:R}:{foreground}:{background}:{text}");
        textures.Add(new(role, key, Model(plane, SceneMaterials.Texture(text, foreground, background, area.Width / area.Height))));
    }

    public ImmutableArray<SceneMeshPart> Finish()
    {
        var result = ImmutableArray.CreateBuilder<SceneMeshPart>(meshes.Count + textures.Count);
        foreach (var (key, mesh) in meshes)
        {
            MeshGeometry3D geometry = new()
            {
                Positions = new(mesh.Positions.Select(p => new Point3D(p.X, p.Y, p.Z))),
                TriangleIndices = new(mesh.TriangleIndices),
                Normals = new(mesh.Normals?.Select(p => new Vector3D(p.X, p.Y, p.Z)) ?? []),
                TextureCoordinates = new(mesh.TextureCoordinates?.Select(p => new Point(p.X, p.Y)) ?? [])
            };
            geometry.Freeze();
            result.Add(new(key.Role, key.Color, Model(geometry, SceneMaterials.Solid(key.Color)), key.Pivot));
        }
        result.AddRange(textures);
        return result.MoveToImmutable();
    }

    public static GeometryModel3D Model(Geometry3D geometry, Material material)
    {
        GeometryModel3D result = new(geometry, material);
        result.Freeze();
        return result;
    }

    public static LayoutRect Inset(LayoutRect r, double x, double y) =>
        new(r.X + x, r.Y + y, r.Width - 2 * x, r.Height - 2 * y);

    public static LayoutRect Center(LayoutRect r, double width, double height) =>
        Inset(r, (r.Width - width) / 2, (r.Height - height) / 2);

    private MeshBuilder Mesh(string color, string role, Point3D pivot)
    {
        var key = (color, role, pivot);
        if (!meshes.TryGetValue(key, out var result)) meshes[key] = result = new();
        return result;
    }
}
