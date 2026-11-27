using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.App.Controls.ThreeD;

internal static class SceneShadow
{
    private const int TextureSize = 64;
    private static readonly Lazy<Material> Ink = new(CreateMaterial);

    internal static GeometryModel3D? Create(SceneNode node)
    {
        var body = node.Body;
        // Planes projected onto perforated plastic intersect the walls and markings of the wells.
        if (node.Pose.Origin.Z > 0 || node.Layer == SceneLayer.Wire || body.Width <= 0 || body.Height <= 0) return null;
        var spread = node.Layer is SceneLayer.Board or SceneLayer.Breadboard ? 2.4 : 1.25;
        var area = SceneMeshes.Inset(body, -spread, -spread);
        var x = area.X + spread * .38;
        var endX = x + area.Width;
        var y = area.Y + spread * .54;
        var endY = y + area.Height;
        // Keep the two large shadows below the worktop detail and on independent depth planes.
        var z = node.Layer switch
        {
            SceneLayer.Breadboard => -.34,
            SceneLayer.Board => -.22,
            _ => -.10
        };
        var mesh = new MeshGeometry3D
        {
            Positions = new([new(x, -y, z), new(x, -endY, z), new(endX, -endY, z), new(endX, -y, z)]),
            TriangleIndices = new([0, 1, 2, 0, 2, 3]),
            TextureCoordinates = new([new(0, 0), new(0, 1), new(1, 1), new(1, 0)])
        };
        mesh.Freeze();
        return SceneMeshes.Model(mesh, Ink.Value);
    }

    private static Material CreateMaterial()
    {
        var pixels = new byte[TextureSize * TextureSize * 4];
        for (var y = 0; y < TextureSize; y++)
            for (var x = 0; x < TextureSize; x++)
            {
                var dx = Math.Max(Math.Abs((x + .5) / TextureSize - .5) - .31, 0);
                var dy = Math.Max(Math.Abs((y + .5) / TextureSize - .5) - .31, 0);
                var ramp = Math.Clamp(1 - Math.Sqrt(dx * dx + dy * dy) / .17, 0, 1);
                pixels[(y * TextureSize + x) * 4 + 3] = (byte)Math.Round(103 * ramp * ramp * (3 - 2 * ramp));
            }
        var texture = BitmapSource.Create(TextureSize, TextureSize, 96, 96, PixelFormats.Pbgra32, null, pixels, TextureSize * 4);
        texture.Freeze();
        var material = new DiffuseMaterial(new ImageBrush(texture));
        material.Freeze();
        return material;
    }
}
