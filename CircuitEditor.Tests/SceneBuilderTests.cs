using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Scene;
using CircuitEditor.Core.Services;
using CircuitEditor.Core.Wiring;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

public sealed class SceneBuilderTests
{
    private readonly SceneBuilder builder = new(new JsonSceneCatalogRepository());

    [Fact]
    public void UnoLedResistorAndWireFollowMockupAndBreadboardBindings()
    {
        var project = BreadboardTestData.Create();
        var board = project.Breadboards[0];
        var uno = BreadboardTestData.Add(project, "arduino-uno");
        project.MockupLayout.Set(uno.Id, new(7, 10, 270));
        project.SchematicLayout.Set(uno.Id, new(1000, 1000));
        var resistor = BreadboardTestData.Add(project);
        Insert(project, resistor, "left/E/07", 90);
        var led = BreadboardTestData.Add(project, "led");
        Insert(project, led, "left/A/11", 90);
        var wire = new Wire(WireEndpoint.Contact(uno.Id, "D13"), WireEndpoint.Hole(board.Id, "left/E/07"),
            controlPoints: [new(60, 9), new(83, 29)]);
        project.Circuit.PutWire(wire);
        project.TransformBoard(board.Id, 112, 20, 90);

        var scene = builder.Build(project);
        Assert.Equal(5, scene.Nodes.Length);
        Assert.True(scene.Bounds.Max.X < 1000);
        var p = scene.Nodes.Single(node => node.Id == uno.Id);
        Assert.Equal(270, p.Pose.Rotation);
        Assert.Equal(SceneLayer.Board, p.Layer);
        var holder = scene.Nodes.Single(node => node.Id == board.Id);
        var wireNode = scene.Nodes.Single(node => node.Id == wire.Id);
        var start = p.Pose.World(p.Contacts.Single(contact => contact.Id == "D13").Position);
        var end = holder.Pose.World(holder.Contacts.Single(hole => hole.Id == "left/E/07").Position);
        Assert.Equal(start, wireNode.Path[0]);
        Assert.Equal(end, wireNode.Path[^1]);
        Assert.Equal(start with { Z = start.Z + SceneContactDepth.Insertion }, wireNode.Path[1]);
        Assert.Equal(end with { Z = end.Z + SceneContactDepth.Insertion }, wireNode.Path[^2]);
        Assert.Equal(wire.Color, wireNode.Color);
        Assert.Contains(wireNode.Path, pt => pt.Z > end.Z + wireNode.WireRadius);
        Assert.Contains(wireNode.Path, pt => pt.X == wire.ControlPoints[0].X && pt.Y == wire.ControlPoints[0].Y);
        var mounted = scene.Nodes.Single(node => node.Id == resistor.Id);
        var contact = mounted.Contacts.Single(item => item.HoleId == "left/E/07");
        Assert.Equal(board.Id, contact.BoardId);
        var tip = mounted.Pose.World(contact.Position);
        Assert.Equal(end.X, tip.X, 8);
        Assert.Equal(end.Y, tip.Y, 8);
        Assert.Equal(end.Z, tip.Z, 8);
        var mountedLed = scene.Nodes.Single(node => node.Id == led.Id);
        Assert.All(mountedLed.Contacts, pin => Assert.Equal(-SceneContactDepth.Insertion, pin.Position.Z, 8));
        Assert.Equal(holder.Profile.Height, mountedLed.Pose.Origin.Z, 8);
        Assert.True(scene.Bounds.Max.Z >= wireNode.Path.Max(pt => pt.Z));
    }

    [Fact]
    public void EspAndI2cModuleKeepFootprintDimensionsAndContacts()
    {
        var project = BreadboardTestData.Create();
        var esp = BreadboardTestData.Add(project, "esp32-devkit");
        var lcd = BreadboardTestData.Add(project, "lcd1602-i2c");
        project.MockupLayout.Set(lcd.Id, new(75, 15, 180));
        var espContact = BreadboardTestData.Footprint(project, "esp32-devkit").Contacts.Single(c => c.ElectricalPinId == "GPIO21");
        var lcdContact = BreadboardTestData.Footprint(project, "lcd1602-i2c").Contacts.Single(c => c.ElectricalPinId == "SDA");
        project.Circuit.PutWire(new(WireEndpoint.Contact(esp.Id, espContact.Id), WireEndpoint.Contact(lcd.Id, lcdContact.Id)));
        var scene = builder.Build(project);
        Assert.Equal(4, scene.Nodes.Length);
        var screen = scene.Nodes.Single(node => node.Id == lcd.Id);
        Assert.Equal(80, screen.Body.Width);
        Assert.Equal(180, screen.Pose.Rotation);
        Assert.Equal(SceneShape.Board, screen.Profile.Shape);
        Assert.Equal(4, screen.Contacts.Length);
    }

    [Fact]
    public void CustomDipUsesProjectFootprintAndBindsAcrossChannel()
    {
        var project = BreadboardTestData.Create();
        var families = new JsonLogicFamilyRepository();
        var chip = new JsonChipTemplateRepository(families).Load().Value!.Single(item => item.Name == "7400");
        project.RegisterChip(chip);
        var part = project.AddComponent(ChipPackage.Component(chip, families.Load().Value![chip.Family]));
        Insert(project, part, "left/E/02", 0);

        var scene = builder.Build(project);
        Assert.Equal(2, scene.Nodes.Length);
        var dip = scene.Nodes.Single(node => node.Id == part.Id);
        Assert.Equal("dip-chip", dip.Family);
        Assert.Equal(14, dip.Contacts.Length);
        Assert.Equal("right/F/02", dip.Contacts.Single(pin => pin.Id == "14").HoleId);
        Assert.Equal("7400", dip.Name);
    }

    [Fact]
    public void EveryBuiltInFootprintHasAProfileAndCancellationIsHonored()
    {
        var profiles = new JsonSceneCatalogRepository().Load().Value!;
        var footprints = new JsonFootprintRepository().Load().Value!;
        Assert.All(footprints, item => Assert.Contains(item.Renderer, profiles.Profiles.Keys));
        Assert.Throws<OperationCanceledException>(() => builder.Build(new(), new CancellationToken(true)));
    }

    private static void Insert(CircuitProject project, Component part, string holeId, int rotation)
    {
        var board = project.Breadboards[0];
        var hole = board.Definition.Holes.Single(item => item.Id == holeId);
        var location = BreadboardGeometry.World(board, hole.Position);
        var footprint = project.Footprints.Single(item => item.ComponentDefinitionId == part.Definition.Id);
        var result = new ComponentInsertionService().Insert(project, part.Id, footprint, board.Id, new(location.X, location.Y, rotation));
        Assert.True(result.Success, result.Error);
    }
}
