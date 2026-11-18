using System.Text.Json;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Services;

namespace CircuitEditor.Tests;

public sealed class ComponentInsertionTests
{
    private readonly ComponentInsertionService service = new();

    [Theory]
    [InlineData("resistor", 4)]
    [InlineData("resistor", 5)]
    [InlineData("resistor", 6)]
    [InlineData("resistor", 7)]
    [InlineData("resistor", 8)]
    [InlineData("resistor", 9)]
    [InlineData("resistor", 10)]
    [InlineData("resistor", 11)]
    [InlineData("resistor", 12)]
    [InlineData("diode", 3)]
    [InlineData("diode", 7)]
    [InlineData("diode", 12)]
    public void EveryAllowedFlexibleSpanCanBeInserted(string id, int span)
    {
        var project = BreadboardTestData.Create(); var component = BreadboardTestData.Add(project, id);
        var board = project.Breadboards[0]; var footprint = BreadboardTestData.Footprint(project, id);
        var hole = board.Definition.Holes.Single(hole => hole.Id == "left/A/01");
        var pose = new ComponentPlacement(board.X + hole.Position.X, board.Y + hole.Position.Y, 90, span);
        var before = JsonSerializer.Serialize(project);
        var preview = service.Preview(project, component.Id, footprint, board.Id, pose);
        Assert.True(preview.Success, preview.Error);
        Assert.Equal(before, JsonSerializer.Serialize(project));
        var result = service.Insert(project, component.Id, footprint, board.Id, pose);
        Assert.True(result.Success, result.Error); Assert.Equal(2, project.Bindings.Count);
        Assert.Equal($"left/A/{span + 1:D2}", project.Bindings.Single(binding => binding.ContactId == "lead-2").HoleId);
    }

    [Theory]
    [InlineData("resistor", 3)]
    [InlineData("resistor", 13)]
    [InlineData("diode", 2)]
    [InlineData("diode", 13)]
    public void InvalidSpanIsRejectedAtomicallyAndPreservesExistingInsertion(string id, int span)
    {
        var project = BreadboardTestData.Create(); var component = BreadboardTestData.Add(project, id);
        var board = project.Breadboards[0]; var footprint = BreadboardTestData.Footprint(project, id);
        var hole = board.Definition.Holes.Single(hole => hole.Id == "left/A/01");
        var pose = new ComponentPlacement(board.X + hole.Position.X, board.Y + hole.Position.Y, 90, 4);
        Assert.True(service.Insert(project, component.Id, footprint, board.Id, pose).Success);
        var before = JsonSerializer.Serialize(project);
        var result = service.Insert(project, component.Id, footprint, board.Id,
            new ComponentPlacement(pose.X, pose.Y, 90, span));
        Assert.False(result.Success); Assert.Empty(result.Bindings);
        Assert.Equal(before, JsonSerializer.Serialize(project));
    }

    [Fact]
    public void OccupiedHoleAndMisalignmentRejectWholeInsertion()
    {
        var project = BreadboardTestData.Create();
        var first = BreadboardTestData.Add(project); var second = BreadboardTestData.Add(project);
        var board = project.Breadboards[0]; var footprint = BreadboardTestData.Footprint(project);
        var hole = board.Definition.Holes.Single(hole => hole.Id == "left/D/05");
        var pose = new ComponentPlacement(board.X + hole.Position.X, board.Y + hole.Position.Y, 0, 4);
        Assert.True(service.Insert(project, first.Id, footprint, board.Id, pose).Success);
        var snapshot = JsonSerializer.Serialize(project);
        Assert.False(service.Insert(project, second.Id, footprint, board.Id, pose).Success);
        Assert.Equal(snapshot, JsonSerializer.Serialize(project));
        Assert.True(service.Insert(project, first.Id, footprint, board.Id, pose).Success);
        snapshot = JsonSerializer.Serialize(project);
        Assert.False(service.Insert(project, first.Id, footprint, board.Id,
            new ComponentPlacement(pose.X + 1, pose.Y, 0, 4)).Success);
        Assert.Equal(snapshot, JsonSerializer.Serialize(project));
    }

    [Fact]
    public void FailureOnSecondContactDoesNotReserveTheFirstFreeHole()
    {
        var project = BreadboardTestData.Create(); var resistor = BreadboardTestData.Add(project);
        var board = project.Breadboards[0];
        var occupied = board.Definition.Holes.Single(hole => hole.Id == "left/D/05");
        Assert.True(service.Insert(project, resistor.Id, BreadboardTestData.Footprint(project), board.Id,
            new ComponentPlacement(board.X + occupied.Position.X, board.Y + occupied.Position.Y, spanHoles: 4)).Success);
        var led = BreadboardTestData.Add(project, "led");
        var free = board.Definition.Holes.Single(hole => hole.Id == "left/C/05");
        var before = JsonSerializer.Serialize(project);
        Assert.False(service.Insert(project, led.Id, BreadboardTestData.Footprint(project, "led"), board.Id,
            new ComponentPlacement(board.X + free.Position.X, board.Y + free.Position.Y)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(project));
        Assert.DoesNotContain(project.Bindings, binding => binding.HoleId == free.Id);
    }

    [Fact]
    public void FixedButtonBindsFourContactsToTwoElectricalPinsAcrossChannel()
    {
        var project = BreadboardTestData.Create(); var component = BreadboardTestData.Add(project, "button");
        var board = project.Breadboards[0]; var footprint = BreadboardTestData.Footprint(project, "button");
        var hole = board.Definition.Holes.Single(hole => hole.Id == "left/E/03");
        var pose = new ComponentPlacement(board.X + hole.Position.X, board.Y + hole.Position.Y);
        Assert.True(service.Insert(project, component.Id, footprint, board.Id, pose).Success);
        Assert.Equal(4, project.Bindings.Count);
        Assert.Equal(2, footprint.Contacts.Select(contact => contact.ElectricalPinId).Distinct().Count());
        Assert.Contains(project.Bindings, binding => binding.HoleId == "right/F/05");
        var snapshot = JsonSerializer.Serialize(project);
        Assert.False(service.Insert(project, component.Id, footprint, board.Id,
            new ComponentPlacement(pose.X, pose.Y, spanHoles: 4)).Success);
        Assert.Equal(snapshot, JsonSerializer.Serialize(project));
    }

    [Fact]
    public void MissingSecondHoleAndFreeMountedUnoDoNotInsert()
    {
        var project = BreadboardTestData.Create(); var component = BreadboardTestData.Add(project, "led");
        var board = project.Breadboards[0]; var footprint = BreadboardTestData.Footprint(project, "led");
        var hole = board.Definition.Holes.Single(hole => hole.Id == "left/E/01");
        Assert.False(service.Insert(project, component.Id, footprint, board.Id,
            new ComponentPlacement(board.X + hole.Position.X, board.Y + hole.Position.Y)).Success);
        Assert.Empty(project.Bindings);
        var uno = BreadboardTestData.Add(project, "arduino-uno");
        Assert.False(service.Insert(project, uno.Id, BreadboardTestData.Footprint(project, "arduino-uno"),
            board.Id, new ComponentPlacement()).Success);
        Assert.Empty(project.Bindings);
    }
}
