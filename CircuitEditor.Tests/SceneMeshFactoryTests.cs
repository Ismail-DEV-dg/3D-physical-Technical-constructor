using CircuitEditor.App.Controls.ThreeD;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Scene;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

public sealed class SceneMeshFactoryTests
{
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
        Assert.Contains(prepared.SelectMany(parts => parts), part => part.Role == "screen");
        Assert.Contains(prepared.SelectMany(parts => parts), part => part.Role == "servo");
    }
}
