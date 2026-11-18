using System.Text.Json;
using System.IO;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Services;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

public sealed class BatchBFootprintTests
{
    [Theory]
    [InlineData("buzzer", "left/E/05", 0, 2)]
    [InlineData("relay", "left/C/02", 0, 5)]
    [InlineData("capacitor", "left/E/02", 90, 2)]
    [InlineData("inductor", "left/E/02", 90, 2)]
    [InlineData("diode", "left/E/02", 90, 2)]
    [InlineData("potentiometer", "left/E/02", 90, 3)]
    [InlineData("npn", "left/E/02", 90, 3)]
    public void ContactsInsertIntoIndependentGroupsAndOccupancyIsAtomic(string id, string holeId, int rotation, int count)
    {
        var project = BreadboardTestData.Create(); var item = BreadboardTestData.Add(project, id);
        var footprint = BreadboardTestData.Footprint(project, id); var board = project.Breadboards[0];
        var hole = board.Definition.Holes.Single(hole => hole.Id == holeId);
        var pose = new ComponentPlacement(board.X + hole.Position.X, board.Y + hole.Position.Y, rotation);
        var service = new ComponentInsertionService();
        var result = service.Insert(project, item.Id, footprint, board.Id, pose);
        Assert.True(result.Success, result.Error); Assert.Equal(count, result.Bindings.Count);
        Assert.Equal(count, result.Bindings.Select(binding => binding.HoleId).Distinct().Count());
        Assert.Equal(count, result.Bindings.Select(binding => board.Definition.Holes.Single(hole => hole.Id == binding.HoleId).GroupId).Distinct().Count());
        var other = BreadboardTestData.Add(project, id); var snapshot = JsonSerializer.Serialize(project);
        Assert.False(service.Insert(project, other.Id, footprint, board.Id, pose).Success);
        Assert.Equal(snapshot, JsonSerializer.Serialize(project));
        Assert.All(footprint.Contacts, contact => Assert.Contains(item.Definition.Pins, pin => pin.Id == contact.ElectricalPinId));
        Assert.NotEqual("placeholder", footprint.Renderer);
    }
    [Theory]
    [InlineData(CapacitorKind.Ceramic)]
    [InlineData(CapacitorKind.Electrolytic)]
    public void BothCapacitorBodiesUseTheSamePhysicalLeads(CapacitorKind kind)
    {
        var project = BreadboardTestData.Create(); var item = BreadboardTestData.Add(project, "capacitor");
        Assert.True(item.TrySetCapacitorKind(kind));
        var board = project.Breadboards[0]; var hole = board.Definition.Holes.Single(hole => hole.Id == "left/E/02");
        Assert.True(new ComponentInsertionService().Insert(project, item.Id, BreadboardTestData.Footprint(project, "capacitor"),
            board.Id, new ComponentPlacement(board.X + hole.Position.X, board.Y + hole.Position.Y, 90)).Success);
    }
    [Fact]
    public void ServoHasThreeCorrectCableContactsButCannotBeInserted()
    {
        var project = BreadboardTestData.Create(); var item = BreadboardTestData.Add(project, "servo");
        var footprint = BreadboardTestData.Footprint(project, "servo");
        Assert.Equal("free", footprint.Mounting);
        Assert.Equal(new[] { "SIG", "VCC", "GND" }, footprint.Contacts.Select(contact => contact.ElectricalPinId));
        var snapshot = JsonSerializer.Serialize(project);
        Assert.False(new ComponentInsertionService().Insert(project, item.Id, footprint, project.Breadboards[0].Id, new()).Success);
        Assert.Equal(snapshot, JsonSerializer.Serialize(project));
    }
    [Theory]
    [InlineData("capacitor")]
    [InlineData("servo")]
    [InlineData("potentiometer")]
    public void MissingPartsOrInvalidKnobAreRejectedByRepository(string id)
    {
        var project = BreadboardTestData.Create();
        var node = JsonSerializer.SerializeToNode(BreadboardTestData.Footprint(project, id))!;
        if (id == "potentiometer") node["RotaryControl"]!["Radius"] = -1;
        else node["Parts"]!.AsObject().Clear();
        using var stream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new { schemaVersion = 1, units = "mm", footprints = new[] { node } }));
        var result = JsonFootprintRepository.Read(stream);
        Assert.False(result.Success); Assert.NotEmpty(result.Error!);
    }
    [Theory]
    [InlineData("diode")]
    [InlineData("inductor")]
    public void AllAllowedAxialSpansInsertAndInvalidSpanKeepsOldBindings(string id)
    {
        var project = BreadboardTestData.Create(); var item = BreadboardTestData.Add(project, id);
        var footprint = BreadboardTestData.Footprint(project, id); var board = project.Breadboards[0];
        var hole = board.Definition.Holes.Single(hole => hole.Id == "left/E/02");
        var service = new ComponentInsertionService();
        for (var span = footprint.MinSpan!.Value; span <= footprint.MaxSpan; span++)
            Assert.True(service.Insert(project, item.Id, footprint, board.Id,
                new ComponentPlacement(board.X + hole.Position.X, board.Y + hole.Position.Y, 90, span)).Success);
        var snapshot = JsonSerializer.Serialize(project);
        Assert.False(service.Insert(project, item.Id, footprint, board.Id,
            new ComponentPlacement(board.X + hole.Position.X, board.Y + hole.Position.Y, 90, footprint.MaxSpan!.Value + 1)).Success);
        Assert.Equal(snapshot, JsonSerializer.Serialize(project));
    }
}
