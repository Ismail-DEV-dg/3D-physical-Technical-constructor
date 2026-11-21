using CircuitEditor.App.Controls;
using CircuitEditor.Core.Layout;

namespace CircuitEditor.Tests;

public sealed class WirePathCacheTests
{
    [Fact]
    public void StaticCurveAndFrozenGeometryAreReusedUntilAnEndpointMoves()
    {
        using var design = new ElectricalTestCircuit();
        var led = design.Add("led");
        var resistor = design.Add("resistor");
        design.Wire(led, "A", resistor, "1");
        var project = design.Project;
        project.Refresh();
        var wire = Assert.Single(project.Model.Circuit.Wires);
        var path = project.Wiring.GetPath(wire)!;
        Assert.Same(path, project.Wiring.GetPath(wire));
        Assert.Same(path, path.WithEnds(null, null));
        var temporary = path.WithEnds(new LayoutPoint(3, 4), null);
        Assert.Equal(new LayoutPoint(3, 4), temporary.Points[0]);
        Assert.NotEqual(temporary.Points[0], path.Points[0]);
        StaRunner.Run(() =>
        {
            var cache = new WireDrawingCache();
            Assert.Same(cache.Get(path), cache.Get(path));
            Assert.True(cache.Get(path).IsFrozen);
            Assert.NotSame(cache.Get(path), cache.Get(temporary));
        });

        project.Model.MockupLayout.Set(led.Model.Id, new(130, 80));
        project.Refresh();
        var moved = project.Wiring.GetPath(wire)!;
        Assert.NotSame(path, moved);
        Assert.Equal(new LayoutPoint(130, 80), moved.Points[0]);
        Assert.Equal(path.Points[^1], moved.Points[^1]);
    }
}
