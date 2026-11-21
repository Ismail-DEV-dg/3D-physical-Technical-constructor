using CircuitEditor.App.Controls.ThreeD;
using CircuitEditor.Core.Scene;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

public sealed class SceneCacheTests
{
    [Fact]
    public async Task MoveReusesMeshAndLeavesOtherNodesIntactButBandChangeInvalidates()
    {
        var project = BreadboardTestData.Create();
        var a = BreadboardTestData.Add(project);
        var b = BreadboardTestData.Add(project);
        var builder = new SceneBuilder(new JsonSceneCatalogRepository());
        var preparation = new ScenePreparation(new SceneMeshFactory());

        var first = await Task.Run(() => preparation.Build(builder.Build(project), default));
        var resistor = first.Nodes.Single(n => n.Node.Id == a.Id);
        var neighbor = first.Nodes.Single(n => n.Node.Id == b.Id);
        Assert.Same(resistor.Parts[0], neighbor.Parts[0]);

        project.MockupLayout.Set(a.Id, new(5, 15, 90));
        var moved = await Task.Run(() => preparation.Build(builder.Build(project), default, first));
        var current = moved.Nodes.Single(n => n.Node.Id == a.Id);
        Assert.Equal(resistor.GeometryKey, current.GeometryKey);
        Assert.Same(resistor.Parts[0], current.Parts[0]);
        Assert.NotSame(resistor.Transform, current.Transform);
        Assert.Same(neighbor, moved.Nodes.Single(n => n.Node.Id == b.Id));

        Assert.True(a.TryUpdateProperties("330 Ω", null, null, out _));
        var changed = await Task.Run(() => preparation.Build(builder.Build(project), default, moved));
        Assert.NotEqual(current.GeometryKey, changed.Nodes.Single(n => n.Node.Id == a.Id).GeometryKey);
    }
}
