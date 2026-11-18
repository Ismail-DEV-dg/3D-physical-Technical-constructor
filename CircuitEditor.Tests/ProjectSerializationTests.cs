using System.Text.Json;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Services;

namespace CircuitEditor.Tests;

public sealed class ProjectSerializationTests
{
    [Fact]
    public void PositionsAndBindingsAreIndependentAndFullCoreProjectRoundTripsThroughJson()
    {
        var project = BreadboardTestData.Create(); var component = BreadboardTestData.Add(project);
        var board = project.Breadboards[0]; var hole = board.Definition.Holes.Single(hole => hole.Id == "left/A/01");
        var service = new ComponentInsertionService();
        Assert.True(service.Insert(project, component.Id, BreadboardTestData.Footprint(project), board.Id,
            new ComponentPlacement(board.X + hole.Position.X, board.Y + hole.Position.Y, 90, 6)).Success);
        var physical = project.MockupLayout.Get(component.Id).Copy();
        project.SchematicLayout.Get(component.Id).MoveTo(400, 600);
        project.SchematicLayout.Get(component.Id).Rotate();
        Assert.Equal(physical.X, project.MockupLayout.Get(component.Id).X);
        Assert.Equal(physical.Rotation, project.MockupLayout.Get(component.Id).Rotation);
        Assert.Equal(2, project.Bindings.Count);
        var json = JsonSerializer.Serialize(project);
        var copy = JsonSerializer.Deserialize<CircuitProject>(json)!;
        Assert.Equal(component.Id, Assert.Single(copy.Circuit.Components).Id);
        Assert.Equal(component.Value, copy.Circuit.Components[0].Value);
        Assert.Equal(400, copy.SchematicLayout.Get(component.Id).X);
        Assert.Equal(600, copy.SchematicLayout.Get(component.Id).Y);
        Assert.Equal(physical.X, copy.MockupLayout.Get(component.Id).X);
        Assert.Equal(6, copy.MockupLayout.Get(component.Id).SpanHoles);
        Assert.Equal(project.Bindings, copy.Bindings);
        Assert.Equal(board.Id, copy.Breadboards[0].Id);
        Assert.Equal(400, copy.Breadboards[0].Definition.Holes.Count);
        Assert.Equal("{}", JsonSerializer.Serialize(copy.Simulation));
        copy.SchematicLayout.Get(component.Id).MoveTo(0, 0);
        Assert.Equal(400, project.SchematicLayout.Get(component.Id).X);
        copy.MockupLayout.Get(component.Id).MoveTo(10, 20);
        Assert.Equal(0, copy.SchematicLayout.Get(component.Id).X);
        Assert.Equal(0, copy.SchematicLayout.Get(component.Id).Y);
        Assert.Equal(600, project.SchematicLayout.Get(component.Id).Y);
        Assert.Null(typeof(Component).GetProperty("X"));
        Assert.Null(typeof(Component).GetProperty("Rotation"));
        Assert.Null(typeof(Pin).GetProperty("X"));
        Assert.DoesNotContain(typeof(Component).Assembly.GetReferencedAssemblies(), assembly =>
            assembly.Name is "PresentationFramework" or "WindowsBase" or "CircuitEditor.App");
        copy.RemoveComponent(component.Id);
        Assert.Empty(copy.Bindings);
        var next = copy.AddComponent(component.Definition);
        Assert.Equal("R2", next.Reference);
    }
}
