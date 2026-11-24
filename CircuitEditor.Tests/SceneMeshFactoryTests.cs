using CircuitEditor.App.Controls.ThreeD;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Scene;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

public sealed class SceneMeshFactoryTests
{
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
