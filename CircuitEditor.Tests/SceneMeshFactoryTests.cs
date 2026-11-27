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

    [Fact]
    public void BreadboardShellOwnsAllFourOutsideWalls()
    {
        StaRunner.Run(() =>
        {
            var project = BreadboardTestData.Create();
            var node = new SceneBuilder(new JsonSceneCatalogRepository()).Build(project).Nodes.Single();
            var parts = new SceneMeshFactory().Build(node, default);
            var body = node.Body;
            var sideHeight = node.Profile.Height - .5;
            var nearEnd = body.Width * .15;
            Point3D[] walls =
            [
                new(body.X, -(body.Y + body.Height / 2), sideHeight),
                new(body.Right, -(body.Y + body.Height / 2), sideHeight),
                new(body.X + nearEnd, -body.Y, sideHeight),
                new(body.X + nearEnd, -body.Bottom, sideHeight)
            ];
            Assert.All(walls, point =>
                Assert.Equal("#DBDAD2", Assert.Single(parts, part => ContainsPoint((MeshGeometry3D)part.Model.Geometry, point)).MaterialKey));
        });
    }

    [Fact]
    public void BatteryTerminalFaceBelongsToTheMetalCap()
    {
        StaRunner.Run(() =>
        {
            var project = BreadboardTestData.Create();
            var battery = BreadboardTestData.Add(project, "battery");
            var node = new SceneBuilder(new JsonSceneCatalogRepository()).Build(project).Nodes.Single(item => item.Id == battery.Id);
            var cell = node.Parts["cell"];
            var terminalFace = new Point3D(cell.Right, -(cell.Y + cell.Height / 2), node.Profile.Clearance + node.Profile.Height / 2);
            var parts = new SceneMeshFactory().Build(node, default);
            Assert.Equal(SceneMeshes.Metal, Assert.Single(parts,
                part => ContainsPoint((MeshGeometry3D)part.Model.Geometry, terminalFace)).MaterialKey);
        });
    }

    [Theory]
    [InlineData("arduino-uno", 10.75, -26, 5.5, SceneMeshes.Metal)]
    [InlineData("arduino-uno", 11.28, -44, 5.5, "#19222B")]
    [InlineData("arduino-nano", 11.42, 1.9, 3.8, SceneMeshes.Metal)]
    [InlineData("esp32-devkit", 16.70, -38.15, 4, SceneMeshes.Metal)]
    public void ControllerPortSideWallsDoNotOverlapTheBacking(string id, double x, double y, double z, string material)
    {
        StaRunner.Run(() =>
        {
            var project = BreadboardTestData.Create();
            var controller = BreadboardTestData.Add(project, id);
            var node = new SceneBuilder(new JsonSceneCatalogRepository()).Build(project).Nodes.Single(item => item.Id == controller.Id);
            var parts = new SceneMeshFactory().Build(node, default);
            Assert.Equal(material, Assert.Single(parts,
                part => ContainsPoint((MeshGeometry3D)part.Model.Geometry, new(x, y, z))).MaterialKey);
        });
    }

    private static bool ContainsAtHeight(MeshGeometry3D[] meshes, double x, double y, double height) =>
        meshes.Any(mesh => ContainsPoint(mesh, new(x, y, height)));

    private static bool ContainsPoint(MeshGeometry3D mesh, Point3D point)
    {
        const double tolerance = 1e-4;
        var indices = mesh.TriangleIndices;
        var vertices = mesh.Positions;
        for (var i = 0; i < indices.Count; i += 3)
        {
            var a = vertices[indices[i]];
            var b = vertices[indices[i + 1]];
            var c = vertices[indices[i + 2]];
            var normal = Vector3D.CrossProduct(b - a, c - a);
            if (normal.LengthSquared < tolerance * tolerance) continue;
            normal.Normalize();
            if (Math.Abs(Vector3D.DotProduct(point - a, normal)) > tolerance) continue;
            if (Inside(a, b) && Inside(b, c) && Inside(c, a)) return true;

            bool Inside(Point3D start, Point3D end) =>
                Vector3D.DotProduct(Vector3D.CrossProduct(end - start, point - start), normal) >= -tolerance;
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
