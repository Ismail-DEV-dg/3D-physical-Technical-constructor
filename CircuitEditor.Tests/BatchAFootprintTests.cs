using System.Text.Json;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Services;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

public sealed class BatchAFootprintTests
{
    [Theory]
    [InlineData("arduino-nano", "left/B/02", 0, 30)]
    [InlineData("esp32-devkit", "left/A/02", 0, 30)]
    [InlineData("lcd1602", "left/E/02", 90, 16)]
    [InlineData("lcd1602-i2c", "left/E/02", 90, 4)]
    [InlineData("oled-ssd1306", "left/E/02", 90, 4)]
    [InlineData("seven-segment", "right/G/02", 90, 10)]
    public void AllContactsInsertWithoutShortingIndependentSignals(string id, string anchorId, int rotation, int count)
    {
        var project = BreadboardTestData.Create();
        var component = BreadboardTestData.Add(project, id);
        var footprint = BreadboardTestData.Footprint(project, id);
        var board = project.Breadboards[0];
        var anchor = board.Definition.Holes.Single(hole => hole.Id == anchorId);
        var pose = new ComponentPlacement(board.X + anchor.Position.X, board.Y + anchor.Position.Y, rotation);
        var service = new ComponentInsertionService();
        var result = service.Insert(project, component.Id, footprint, board.Id, pose);
        Assert.True(result.Success, result.Error);
        Assert.Equal(count, result.Bindings.Count);
        Assert.Equal(count, result.Bindings.Select(binding => binding.HoleId).Distinct().Count());
        foreach (var group in result.Bindings.GroupBy(binding =>
            board.Definition.Holes.Single(hole => hole.Id == binding.HoleId).GroupId))
        {
            // Different electrical signals must never share one connected breadboard group.
            Assert.Single(group.Select(binding => footprint.Contacts.Single(contact => contact.Id == binding.ContactId)
                .ElectricalPinId).Distinct());
        }
        var expected = footprint.Contacts.Select(contact => contact.ElectricalPinId).Distinct().Order();
        Assert.Equal(expected, component.Definition.Pins.Where(pin => pin.IsExposed).Select(pin => pin.Id).Order());
        var snapshot = JsonSerializer.Serialize(project);
        Assert.False(service.Insert(project, component.Id, footprint, board.Id,
            new ComponentPlacement(pose.X + 1, pose.Y, rotation)).Success);
        Assert.Equal(snapshot, JsonSerializer.Serialize(project));
    }

    [Theory]
    [InlineData("arduino-nano", "left/B/02", 0)]
    [InlineData("esp32-devkit", "left/A/02", 0)]
    [InlineData("lcd1602", "left/E/02", 90)]
    [InlineData("lcd1602-i2c", "left/E/02", 90)]
    [InlineData("oled-ssd1306", "left/E/02", 90)]
    [InlineData("seven-segment", "right/G/02", 90)]
    public void OccupiedLastContactRejectsWholeBatchInsertion(string id, string anchorId, int rotation)
    {
        var project = BreadboardTestData.Create();
        var component = BreadboardTestData.Add(project, id);
        var footprint = BreadboardTestData.Footprint(project, id);
        var board = project.Breadboards[0];
        var anchor = board.Definition.Holes.Single(hole => hole.Id == anchorId);
        var pose = new ComponentPlacement(board.X + anchor.Position.X, board.Y + anchor.Position.Y, rotation);
        var last = ContactGeometry.World(footprint, footprint.Contacts[^1], pose, board.Definition.HolePitch);
        var led = BreadboardTestData.Add(project, "led");
        var service = new ComponentInsertionService();
        Assert.True(service.Insert(project, led.Id, BreadboardTestData.Footprint(project, "led"), board.Id,
            new ComponentPlacement(last.X, last.Y, 90)).Success);
        var snapshot = JsonSerializer.Serialize(project);
        var result = service.Insert(project, component.Id, footprint, board.Id, pose);
        Assert.False(result.Success); Assert.Empty(result.Bindings);
        Assert.Equal(snapshot, JsonSerializer.Serialize(project));
    }

    [Fact]
    public void PhysicalPinNumbersMapToActualHeaders()
    {
        var footprints = new JsonFootprintRepository().Load().Value!;
        var nano = footprints.Single(item => item.ComponentDefinitionId == "arduino-nano");
        Assert.Equal("D1", nano.Contacts.Single(contact => contact.Id == "1").ElectricalPinId);
        Assert.Equal("VIN", nano.Contacts.Single(contact => contact.Id == "30").ElectricalPinId);
        Assert.Equal(new[] { "3", "28" }, nano.Contacts.Where(contact => contact.ElectricalPinId == "RESET").Select(contact => contact.Id));
        Assert.Equal(15.24, nano.Contacts.Max(contact => contact.X));
        var esp = footprints.Single(item => item.ComponentDefinitionId == "esp32-devkit");
        Assert.Equal(25.4, esp.Contacts.Max(contact => contact.X));
        Assert.DoesNotContain(esp.Contacts, contact => contact.ElectricalPinId == "GPIO0");
        var segment = footprints.Single(item => item.ComponentDefinitionId == "seven-segment");
        Assert.Equal(new[] { "8", "3" }, segment.Contacts.Where(contact => contact.ElectricalPinId == "COM").Select(contact => contact.Id));
        Assert.Equal("DP", segment.Contacts.Single(contact => contact.Id == "5").ElectricalPinId);
        Assert.Equal("A", segment.Contacts.Single(contact => contact.Id == "7").ElectricalPinId);
        Assert.Equal(27, footprints.Count(item => item.Renderer != "placeholder"));
        Assert.DoesNotContain(footprints, item => item.Renderer == "placeholder");
    }
}
