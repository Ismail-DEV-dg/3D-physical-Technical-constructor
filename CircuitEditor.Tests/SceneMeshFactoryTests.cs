using System.Windows.Media.Media3D;
using CircuitEditor.App.Controls.ThreeD;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Scene;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

public sealed class SceneMeshFactoryTests
{
    [Theory]
    [InlineData("dht11")]
    [InlineData("dht22")]
    public void SensorVentFloorHasOnlyOneVisibleSurface(string partId)
    {
        StaRunner.Run(() =>
        {
            var project = BreadboardTestData.Create();
            var sensor = BreadboardTestData.Add(project, partId);
            var node = new SceneBuilder(new JsonSceneCatalogRepository()).Build(project).Nodes.Single(item => item.Id == sensor.Id);
            var top = node.Profile.Clearance + node.Profile.Height;
            var ventX = node.Body.X + node.Body.Width * (.09 + .205 / 2);
            var ventY = node.Body.Y + node.Body.Height * (.07 + .125 / 2);
            var parts = new SceneMeshFactory().Build(node, default);
            Assert.Single(parts, part => part.Model.Geometry is MeshGeometry3D mesh && ContainsAtHeight([mesh], ventX, -ventY, top - .88));
            Assert.DoesNotContain(parts, part => part.Model.Geometry is MeshGeometry3D mesh && ContainsAtHeight([mesh], ventX, -ventY, top));
        });
    }

    [Fact]
    public void LedBodyIsCenteredAndLegsRemainOnTheirContactAxes()
    {
        StaRunner.Run(() =>
        {
            var project = BreadboardTestData.Create();
            var led = BreadboardTestData.Add(project, "led");
            var node = new SceneBuilder(new JsonSceneCatalogRepository()).Build(project).Nodes.Single(item => item.Id == led.Id);
            Assert.Equal(node.Contacts.Average(pin => pin.Position.Y), node.Body.Y + node.Body.Height / 2, 6);
            var lead = Assert.Single(new SceneMeshFactory().Build(node, default), part => part.Role == "lead");
            var geometry = Assert.IsType<MeshGeometry3D>(lead.Model.Geometry);
            Assert.All(geometry.Positions, point => Assert.Contains(node.Contacts,
                pin => Math.Abs(point.X - pin.Position.X) <= .24 && Math.Abs(point.Y + pin.Position.Y) <= .24));
            Assert.Equal(node.Contacts.Min(pin => pin.Position.Z), lead.Model.Bounds.Z, 5);
        });
    }

    [Fact]
    public void SocketRimsLeaveOpeningsAboveRecessedFloors()
    {
        StaRunner.Run(() =>
        {
            var project = BreadboardTestData.Create();
            BreadboardTestData.Add(project, "arduino-uno");
            var builder = new SceneBuilder(new JsonSceneCatalogRepository());
            var factory = new SceneMeshFactory();
            foreach (var node in builder.Build(project).Nodes)
            {
                var holes = node.Layer == SceneLayer.Breadboard;
                var pin = holes ? node.Contacts[0] : node.Contacts.Single(p => p.Id == "D13");
                var opening = pin.Position;
                var rim = opening.Z + SceneContactDepth.Insertion;
                var floor = rim - (holes ? SceneContactDepth.BreadboardBore : SceneContactDepth.UnoSocketBore);
                var meshes = factory.Build(node, default).Where(part => part.MaterialKey != SceneMeshes.ArtworkMaterial)
                    .Select(part => (MeshGeometry3D)part.Model.Geometry).ToArray();
                Assert.False(ContainsAtHeight(meshes, opening.X, -opening.Y, rim));
                Assert.True(ContainsAtHeight(meshes, opening.X + .8, -opening.Y, rim));
                Assert.True(ContainsAtHeight(meshes, opening.X, -opening.Y, floor));
            }
        });
    }

    private static bool ContainsAtHeight(MeshGeometry3D[] meshes, double x, double y, double height)
    {
        const double tolerance = 1e-4;
        double Side(Point3D a, Point3D b) => (b.X - a.X) * (y - a.Y) - (b.Y - a.Y) * (x - a.X);
        foreach (var mesh in meshes)
        {
            var indices = mesh.TriangleIndices;
            var vertices = mesh.Positions;
            for (var i = 0; i < indices.Count; i += 3)
            {
                var a = vertices[indices[i]];
                var b = vertices[indices[i + 1]];
                var c = vertices[indices[i + 2]];
                if (Math.Abs(a.Z - height) > tolerance || Math.Abs(b.Z - height) > tolerance ||
                    Math.Abs(c.Z - height) > tolerance) continue;
                var sides = new[] { Side(a, b), Side(b, c), Side(c, a) };
                if (sides.All(v => v >= -tolerance) || sides.All(v => v <= tolerance)) return true;
            }
        }
        return false;
    }

    [Fact]
    public void CylinderBodyKeepsTheSpecifiedMillimetreDiameter()
    {
        StaRunner.Run(() =>
        {
            SceneMeshes meshes = new();
            meshes.Disc(0, 0, 0, 7, 4, "#353D45");
            var bounds = Assert.Single(meshes.Finish()).Model.Bounds;
            Assert.Equal(4, bounds.SizeX, 5);
            Assert.Equal(4, bounds.SizeY, 5);
            Assert.Equal(7, bounds.SizeZ, 5);
        });
    }

    [Fact]
    public async Task AllLibraryModelsAndBreadboardCrossThreadsAsFrozenMeshes()
    {
        var project = BreadboardTestData.Create();
        foreach (var footprint in project.Footprints)
            BreadboardTestData.Add(project, footprint.ComponentDefinitionId);
        var capacitor = BreadboardTestData.Add(project, "capacitor");
        capacitor.TrySetCapacitorKind(CapacitorKind.Electrolytic);
        var scene = new SceneBuilder(new JsonSceneCatalogRepository()).Build(project);
        var factory = new SceneMeshFactory();
        var uno = scene.Nodes.Single(n => n.Family == "arduino-uno");
        StaRunner.Run(() => _ = factory.Build(uno, default));
        var prepared = await Task.Run(() => scene.Nodes.Select(node => factory.Build(node, default)).ToArray());

        Assert.All(prepared, parts =>
        {
            Assert.NotEmpty(parts);
            Assert.All(parts, part =>
            {
                Assert.True(part.Model.IsFrozen);
                Assert.True(part.Model.Geometry.IsFrozen);
                Assert.True(part.Model.Material.IsFrozen);
                Assert.False(part.Model.Bounds.IsEmpty);
            });
        });
        Assert.InRange(prepared[0].Length, 2, 8);
        Assert.All(scene.Nodes.Zip(prepared).Where(item => item.First.Layer is SceneLayer.Board or SceneLayer.Breadboard ||
            item.First.Family is "lcd1602" or "lcd1602-i2c" or "oled-ssd1306" or "hc-sr04" or "dht11" or "dht22" or "relay"),
            item => Assert.Contains(item.Second, part => part.MaterialKey == SceneMeshes.ArtworkMaterial));
        Assert.Contains(prepared.SelectMany(parts => parts), part => part.Role == "screen");
        Assert.Contains(prepared.SelectMany(parts => parts), part => part.Role == "servo");
    }
}
