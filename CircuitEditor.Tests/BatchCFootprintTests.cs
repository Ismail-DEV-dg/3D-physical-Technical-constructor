using System.Text.Json;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Services;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

public sealed class BatchCFootprintTests
{
    [Theory]
    [InlineData("dht11", 4)] [InlineData("dht22", 4)] [InlineData("hc-sr04", 4)]
    [InlineData("pir", 3)] [InlineData("photoresistor", 2)] [InlineData("thermistor", 2)]
    [InlineData("rgb-led", 4)] [InlineData("switch", 3)] [InlineData("battery", 2)]
    public void EveryNewComponentInsertsAtomicallyWithoutShortingSignals(string id, int expectedCount)
    {
        var project = BreadboardTestData.Create(); var item = BreadboardTestData.Add(project, id);
        var footprint = BreadboardTestData.Footprint(project, id); var board = project.Breadboards[0];
        var holeId = id == "battery" ? "left/A/05" : "left/E/05";
        var hole = board.Definition.Holes.Single(hole => hole.Id == holeId);
        var rotation = id == "battery" ? 0 : 90;
        var pose = new ComponentPlacement(board.X + hole.Position.X, board.Y + hole.Position.Y, rotation);
        var service = new ComponentInsertionService(); var result = service.Insert(project, item.Id, footprint, board.Id, pose);
        Assert.True(result.Success, result.Error); Assert.Equal(expectedCount, result.Bindings.Count);
        Assert.Equal(expectedCount, result.Bindings.Select(binding => binding.HoleId).Distinct().Count());
        Assert.Equal(expectedCount, result.Bindings.Select(binding => board.Definition.Holes.Single(hole => hole.Id == binding.HoleId).GroupId).Distinct().Count());
        var other = BreadboardTestData.Add(project, id); var before = JsonSerializer.Serialize(project);
        Assert.False(service.Insert(project, other.Id, footprint, board.Id, pose).Success);
        Assert.Equal(before, JsonSerializer.Serialize(project));
    }
    [Theory]
    [InlineData("photoresistor")]
    [InlineData("thermistor")]
    public void FlexibleSensorsSupportEverySpanAndRejectOutOfRange(string id)
    {
        var project = BreadboardTestData.Create(); var item = BreadboardTestData.Add(project, id);
        var footprint = BreadboardTestData.Footprint(project, id); var board = project.Breadboards[0];
        var hole = board.Definition.Holes.Single(hole => hole.Id == "left/E/02");
        var service = new ComponentInsertionService();
        for (var span = footprint.MinSpan!.Value; span <= footprint.MaxSpan; span++)
            Assert.True(service.Insert(project, item.Id, footprint, board.Id,
                new ComponentPlacement(board.X + hole.Position.X, board.Y + hole.Position.Y, 90, span)).Success);
        var before = JsonSerializer.Serialize(project);
        Assert.False(service.Insert(project, item.Id, footprint, board.Id,
            new ComponentPlacement(board.X + hole.Position.X, board.Y + hole.Position.Y, 90, footprint.MaxSpan!.Value + 1)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(project));
    }
    [Fact]
    public void LibraryHasAll28RealModelsAndDhtNcIsPhysicallyExposedButNotConnected()
    {
        var library = new JsonComponentLibraryRepository().Load().Components;
        var footprints = new JsonFootprintRepository().Load(); Assert.True(footprints.Success, footprints.Error);
        Assert.Equal(28, footprints.Value!.Count); Assert.DoesNotContain(footprints.Value, item => item.Renderer == "placeholder");
        foreach (var id in new[] { "dht11", "dht22" })
        {
            var nc = library.Single(item => item.Id == id).Pins.Single(pin => pin.Id == "NC");
            Assert.True(nc.IsExposed); Assert.True(nc.IsNoConnect);
            Assert.Equal(new[] { "VCC", "DATA", "NC", "GND" }, footprints.Value.Single(item => item.ComponentDefinitionId == id)
                .Contacts.Select(contact => contact.ElectricalPinId));
        }
        foreach (var footprint in footprints.Value)
            Assert.All(footprint.Contacts, contact => Assert.Contains(library.Single(item => item.Id == footprint.ComponentDefinitionId).Pins,
                pin => pin.Id == contact.ElectricalPinId && pin.IsExposed));
    }
}
