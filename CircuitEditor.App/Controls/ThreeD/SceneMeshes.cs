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
    internal const double RecessBackingClearance = .12;
    internal const string Metal = "#A3ADB4";
    internal const string Dark = "#242936";
    internal const string Gold = "#CBA561";
    internal const string Label = "label";
    internal const string SurfaceRole = "mark";
    internal const string ArtworkMaterial = "artwork";
    private const int RoundSides = 16;
    private const double Detail = .08;
    private readonly Dictionary<(string Color, string Role, Point3D Pivot, SceneFinish Finish), MeshBuilder> meshes = [];
    private readonly List<SceneMeshPart> textures = [];

    public static Point3D Point(ScenePoint p) => new(p.X, -p.Y, p.Z);
    private static Vector3 Vector(double x, double y, double z) => new((float)x, (float)-y, (float)z);

    public void Box(LayoutRect r, double z, double h, string color, string role = "", Point3D pivot = default)
    {
        if (r.Width <= 0 || r.Height <= 0 || h <= 0) return;
        Mesh(color, role, pivot).AddBox(Vector(r.X + r.Width / 2, r.Y + r.Height / 2, z + h / 2),
            (float)r.Width, (float)r.Height, (float)h);
    }

    public void BeveledBox(LayoutRect body, double z, double height, string color, double corner = .7, double bevel = .28,
        string role = "", Point3D pivot = default, SceneFinish finish = SceneFinish.Matte)
    {
        if (body.Width <= 0 || body.Height <= 0 || height <= 0) return;
        SceneBeveledBox.Add(Mesh(color, role, pivot, finish), body, z, height, corner, bevel);
    }

    public SceneSurfaceTexture? BevelWithArtwork(LayoutRect body, double z, double height, string color,
        Action<DrawingContext>? paint)
    {
        BeveledBox(body, z, height, color);
        if (paint is null) return null;
        var texture = new SceneSurfaceTexture(body, paint);
        Face(body, z + height, texture);
        return texture;
    }

    public void Face(LayoutRect area, double z, SceneSurfaceTexture texture, string role = SurfaceRole)
    {
        if (area.Width <= 0 || area.Height <= 0) return;
        const double lift = .025;
        MeshGeometry3D plane = new()
        {
            Positions = new([new(area.X, -area.Y, z + lift), new(area.X, -area.Bottom, z + lift),
                new(area.Right, -area.Bottom, z + lift), new(area.Right, -area.Y, z + lift)]),
            TriangleIndices = new([0, 1, 2, 0, 2, 3]),
            TextureCoordinates = new([new(0, 0), new(0, 1), new(1, 1), new(1, 0)])
        };
        plane.Freeze();
        textures.Add(new(role, ArtworkMaterial, Model(plane, texture.ForRegion(area))));
    }

    public void DiscFace(LayoutRect area, double z, SceneSurfaceTexture texture)
    {
        const int division = 24;
        const double lift = .025;
        var x = area.X + area.Width / 2;
        var y = area.Y + area.Height / 2;
        var points = new Point3DCollection { new(x, -y, z + lift) };
        var uv = new PointCollection { new(.5, .5) };
        var triangles = new Int32Collection();
        for (var i = 0; i < division; i++)
        {
            var angle = -Math.Tau * i / division;
            var u = x + Math.Cos(angle) * area.Width / 2;
            var v = y + Math.Sin(angle) * area.Height / 2;
            points.Add(new(u, -v, z + lift));
            uv.Add(new(.5 + .5 * Math.Cos(angle), .5 + .5 * Math.Sin(angle)));
            triangles.Add(0);
            triangles.Add(i + 1);
            triangles.Add((i + 1) % division + 1);
        }
        var plane = new MeshGeometry3D { Positions = points, TriangleIndices = triangles, TextureCoordinates = uv };
        plane.Freeze();
        textures.Add(new(SurfaceRole, ArtworkMaterial, Model(plane, texture.ForRegion(area))));
    }

    public void Plate(IReadOnlyList<Point> contour, double z, double height, string color, string role)
    {
        var mesh = Mesh(color, role, default);
        var cross = Vector(contour[1].X - contour[0].X, contour[1].Y - contour[0].Y, 0);
        var edge = Vector(contour[2].X - contour[0].X, contour[2].Y - contour[0].Y, 0);
        var points = Vector3.Cross(cross, edge).Z > 0 ? contour.ToArray() : contour.Reverse().ToArray();
        Vector3 At(Point p, double elevation) => Vector(p.X, p.Y, elevation);
        for (var i = 1; i < points.Length - 1; i++)
            mesh.AddTriangle(At(points[0], z + height), At(points[i], z + height), At(points[i + 1], z + height));
        for (var i = 0; i < points.Length; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Length];
            mesh.AddQuad(At(a, z), At(b, z), At(b, z + height), At(a, z + height));
        }
    }

    public void Disc(double x, double y, double z, double h, double d, string color, string role = "",
        Point3D pivot = default, int sides = RoundSides, SceneFinish finish = SceneFinish.Matte) =>
        Rod(new(x, y, z), new(x, y, z + h), d, color, role, pivot, sides, finish);

    public void Rod(ScenePoint a, ScenePoint b, double d, string color, string role = "",
        Point3D pivot = default, int sides = RoundSides, SceneFinish finish = SceneFinish.Matte)
    {
        var from = Vector(a.X, a.Y, a.Z);
        var to = Vector(b.X, b.Y, b.Z);
        if (from != to)
            Mesh(color, role, pivot, finish).AddCylinder(from, to, (float)(d / 2), sides, true, true);
    }

    public void Taper(ScenePoint a, ScenePoint b, double fromDiameter, double toDiameter, string color)
    {
        var start = Vector(a.X, a.Y, a.Z);
        var vector = Vector(b.X, b.Y, b.Z) - start;
        if (vector.LengthSquared() <= 0) return;
        Mesh(color, "", default).AddCone(start, Vector3.Normalize(vector), (float)(fromDiameter / 2),
            (float)(toDiameter / 2), vector.Length(), true, true, 24);
    }

    public void Dome(LayoutRect r, double z, double height, string color, string role = "", SceneFinish finish = SceneFinish.Matte) =>
        Mesh(color, role, default, finish).AddEllipsoid(Vector(r.X + r.Width / 2, r.Y + r.Height / 2, z + height / 2),
            (float)(r.Width / 2), (float)(r.Height / 2), (float)(height / 2), RoundSides, 8);

    public void Lens(double x, double y, double z, double diameter, double height, string color, string role)
    {
        const int slices = 36;
        const int levels = 12;
        var radius = diameter / 2;
        var mesh = Mesh(color, role, default, SceneFinish.Gloss);
        var crown = Math.Min(radius, height * .62);
        var profile = new List<(double Radius, double Z)> { (radius, z), (radius, z + height - crown) };
        for (var level = 1; level < levels; level++)
        {
            var angle = Math.PI * level / (2 * levels);
            profile.Add((radius * Math.Cos(angle), z + height - crown + crown * Math.Sin(angle)));
        }
        Vector3 At(int slice, (double Radius, double Z) ring)
        {
            var angle = Math.Tau * slice / slices;
            return Vector(x + Math.Cos(angle) * ring.Radius, y - Math.Sin(angle) * ring.Radius, ring.Z);
        }
        for (var slice = 0; slice < slices; slice++)
        {
            for (var level = 1; level < profile.Count; level++)
                mesh.AddQuad(At(slice, profile[level - 1]), At(slice + 1, profile[level - 1]),
                    At(slice + 1, profile[level]), At(slice, profile[level]));
            mesh.AddTriangle(At(slice, profile[^1]), At(slice + 1, profile[^1]), Vector(x, y, z + height));
            mesh.AddTriangle(At(slice + 1, profile[0]), At(slice, profile[0]), Vector(x, y, z));
        }
    }

    public void Recess(LayoutRect surface, LayoutRect opening, double top, double depth, string surfaceColor,
        string rimColor, string insideColor, double chamfer = .18)
    {
        var bottom = Inset(opening, chamfer, chamfer);
        var edge = Math.Min(depth * .4, chamfer * 1.45);
        var slab = Mesh(surfaceColor, "", default);
        var rim = Mesh(rimColor, "", default);
        var lining = Mesh(insideColor, "", default);
        Bridge(slab, surface, top, opening, top);
        Bridge(rim, opening, top, bottom, top - edge);
        Bridge(lining, bottom, top - edge, bottom, top - depth);
        Box(bottom, top - depth - .06, .06, insideColor);
    }

    public void SideWalls(LayoutRect surface, double bottom, double top, string color) =>
        Bridge(Mesh(color, "", default), surface, bottom, surface, top);

    public void Ring(double x, double y, double z, double height, double diameter, double opening,
        string color, SceneFinish finish = SceneFinish.Metal)
    {
        const int count = 28;
        var mesh = Mesh(color, "", default, finish);
        for (var i = 0; i < count; i++)
        {
            var a = Math.Tau * i / count;
            var b = Math.Tau * (i + 1) / count;
            Vector3 At(double angle, double radius, double level) =>
                Vector(x + radius * Math.Cos(angle), y - radius * Math.Sin(angle), level);
            var inner = opening / 2;
            var outer = diameter / 2;
            var top = z + height;
            mesh.AddQuad(At(a, outer, top), At(b, outer, top), At(b, inner, top), At(a, inner, top));
            mesh.AddQuad(At(a, outer, z), At(b, outer, z), At(b, outer, top), At(a, outer, top));
            mesh.AddQuad(At(b, inner, z), At(a, inner, z), At(a, inner, top), At(b, inner, top));
        }
    }

    private static void Bridge(MeshBuilder mesh, LayoutRect outer, double top, LayoutRect inner, double bottom)
    {
        Vector3[] Border(LayoutRect box, double z) =>
            [Vector(box.X, box.Y, z), Vector(box.X, box.Bottom, z),
                Vector(box.Right, box.Bottom, z), Vector(box.Right, box.Y, z)];
        var first = Border(outer, top);
        var second = Border(inner, bottom);
        for (var i = 0; i < first.Length; i++)
        {
            var next = (i + 1) % first.Length;
            mesh.AddQuad(first[i], first[next], second[next], second[i]);
        }
    }

    public void Tube(IEnumerable<ScenePoint> path, double diameter, string color, string role = "",
        int sides = 6, SceneFinish finish = SceneFinish.Matte)
    {
        List<Vector3> points = [];
        foreach (var p in path)
        {
            var point = Vector(p.X, p.Y, p.Z);
            if (points.Count == 0 || point != points[^1]) points.Add(point);
        }
        if (points.Count >= 2) Mesh(color, role, default, finish).AddTube(points, (float)diameter, sides, false, true, true);
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
            result.Add(new(key.Role, key.Color, Model(geometry, SceneMaterials.Finished(key.Color, key.Finish)), key.Pivot));
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

    private MeshBuilder Mesh(string color, string role, Point3D pivot, SceneFinish finish = SceneFinish.Matte)
    {
        var key = (color, role, pivot, finish == SceneFinish.Matte && color is Metal or Gold ? SceneFinish.Metal : finish);
        if (!meshes.TryGetValue(key, out var result)) meshes[key] = result = new();
        return result;
    }
}
