using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using CircuitEditor.App.Controls.ThreeD;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Scene;
using CircuitEditor.Core.Services;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

[Trait("Apartment", "STA")]
public sealed class SceneExportTests
{
    [Fact]
    public void RevisedSceneRendersMountedLedFromSavedMockup()
    {
        StaRunner.Run(() =>
        {
            using var environment = new Stage4TestEnvironment();
            var examples = new JsonProjectSampleRepository();
            var blink = examples.Load().Single(sample => sample.File == "uno-blink.circ");
            Assert.True(environment.Session.Load(examples.PathFor(blink)));
            var project = environment.Project.Model;
            var led = project.Circuit.Components.Single(part => part.Definition.Id == "led");
            var footprint = project.Footprints.Single(item => item.ComponentDefinitionId == "led");
            var board = project.Breadboards[0];
            var hole = board.Definition.Holes.Single(item => item.Id == "left/A/11");
            var at = BreadboardGeometry.World(board, hole.Position);
            var insertion = new ComponentInsertionService().Insert(project, led.Id, footprint, board.Id, new(at.X, at.Y, 90));
            Assert.True(insertion.Success, insertion.Error);
            var scene = new SceneBuilder(new JsonSceneCatalogRepository()).Build(project);
            var prepared = new ScenePreparation(new SceneMeshFactory()).Build(scene, default);
            var camera = new PerspectiveCamera { FieldOfView = 42 };
            const int width = 1100;
            const int height = 680;
            SceneCamera.Apply(camera, scene.Bounds, SceneCameraView.Initial, (double)width / height, false);
            var viewport = new Viewport3D { Camera = camera };
            viewport.Children.Add(new ModelVisual3D { Content = prepared.Lights });
            foreach (var node in prepared.Nodes)
            {
                if (SceneShadow.Create(node.Node) is { } shadow)
                    viewport.Children.Add(new ModelVisual3D { Content = shadow, Transform = node.Transform });
                var model = new Model3DGroup { Transform = node.Transform };
                foreach (var part in node.Parts) model.Children.Add(part.Model);
                viewport.Children.Add(new ModelVisual3D { Content = model });
            }
            viewport.Measure(new Size(width, height));
            viewport.Arrange(new Rect(0, 0, width, height));
            viewport.UpdateLayout();
            var png = ScenePng.Capture(viewport, new SolidColorBrush(Color.FromRgb(17, 21, 25)), 1);
            Assert.True(png.Length > 20_000);
            if (Environment.GetEnvironmentVariable("CIRCUIT_EDITOR_3D_REVIEW") is { Length: > 0 } path)
                File.WriteAllBytes(path, ScenePng.Capture(viewport, new SolidColorBrush(Color.FromRgb(17, 21, 25)), 2));
        });
    }

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
