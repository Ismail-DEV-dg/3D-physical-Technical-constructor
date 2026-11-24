using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using CircuitEditor.App.Controls.ThreeD;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

[Trait("Apartment", "STA")]
public sealed class SceneExportTests
{
    [Fact]
    public void ViewportPngScalesAndIncludesGeometry()
    {
        StaRunner.Run(() =>
        {
            var viewport = Viewport();
            var bytes = ScenePng.Capture(viewport, Brushes.Black, 2);
            using var stream = new MemoryStream(bytes);
            var image = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
            Assert.Equal(300, image.PixelWidth);
            Assert.Equal(200, image.PixelHeight);
            var bits = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
            byte[] color = new byte[4];
            bits.CopyPixels(new Int32Rect(150, 100, 1, 1), color, 4, 0);
            Assert.True(color[2] > 100, "The 3D mesh must appear in the viewport PNG.");
            Assert.Throws<ArgumentOutOfRangeException>(() => ScenePng.Capture(viewport, Brushes.Black, 0));
        });
    }

    [Fact]
    public void ObjReferencesMaterialsAndWritesTextureThroughRepository()
    {
        StaRunner.Run(() =>
        {
            using var environment = new Stage4TestEnvironment();
            var obj = Path.Combine(environment.Folder, "scene.obj");
            Assert.Null(SceneObj.Write(Viewport(), obj, new FileExportRepository()));
            var geometry = File.ReadAllText(obj);
            var link = geometry.Split('\n').Single(line => line.StartsWith("mtllib", StringComparison.Ordinal)).Trim();
            Assert.Equal("mtllib ./scene.mtl", link);
            Assert.Contains("usemtl", geometry);
            Assert.Contains("\nv ", geometry);
            var materials = File.ReadAllText(Path.ChangeExtension(obj, ".mtl"));
            Assert.Contains("newmtl", materials);
            var texture = materials.Split('\n').Single(line => line.StartsWith("map_Kd ", StringComparison.Ordinal))[7..].Trim();
            Assert.True(File.Exists(Path.Combine(environment.Folder, texture)), texture);
        });
    }

    [Fact]
    public void RenderedPanelShowsOnlyItsCroppedPartOfTheDrawing()
    {
        StaRunner.Run(() =>
        {
            var viewport = Viewport();
            viewport.Children.RemoveAt(1);
            var face = new SceneSurfaceTexture(new(-4, -4, 8, 8), dc =>
            {
                dc.DrawRectangle(Brushes.Red, null, new Rect(-4, -4, 4, 8));
                dc.DrawRectangle(Brushes.DodgerBlue, null, new Rect(0, -4, 4, 8));
            });
            SceneMeshes meshes = new();
            meshes.Face(new(0, -4, 4, 8), 2, face);
            viewport.Children.Add(new ModelVisual3D { Content = Assert.Single(meshes.Finish()).Model });
            using var stream = new MemoryStream(ScenePng.Capture(viewport, Brushes.Black, 1));
            var frame = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
            var bits = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
            byte[] color = new byte[4];
            foreach (var x in new[] { 78, 87, 94 })
            {
                bits.CopyPixels(new Int32Rect(x, 50, 1, 1), color, 4, 0);
                Assert.True(color[0] > 180 && color[2] < 70, $"Texture crop at {x}: B={color[0]}, R={color[2]}");
            }
        });
    }

    private static Viewport3D Viewport()
    {
        var viewport = new Viewport3D
        {
            Camera = new PerspectiveCamera(new(0, 0, 30), new(0, 0, -30), new(0, 1, 0), 45)
        };
        SceneMeshes meshes = new();
        meshes.Box(new(-4, -4, 8, 8), 0, 2, "#FF0000");
        meshes.Text(new(-8, -2, 2, 3), 2, "WPF", "#00FFFF");
        viewport.Children.Add(new ModelVisual3D { Content = new AmbientLight(Colors.White) });
        foreach (var model in meshes.Finish()) viewport.Children.Add(new ModelVisual3D { Content = model.Model });
        viewport.Measure(new Size(150, 100));
        viewport.Arrange(new Rect(0, 0, 150, 100));
        viewport.UpdateLayout();
        return viewport;
    }
}
