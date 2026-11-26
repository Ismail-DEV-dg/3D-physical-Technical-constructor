using CircuitEditor.App.Interaction;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Wiring;

namespace CircuitEditor.Tests;

public sealed class MockupInspectionTests
{
    [Fact]
    public void NetInspectionFollowsTheWireAndBreadboardGroupWithoutEditingTheProject()
    {
        using var design = new Stage4TestEnvironment();
        var project = design.Project;
        var mockup = design.Mockup;
        var resistor = project.Add(project.Library.Single(part => part.Id == "resistor"))!;
        var contact = WireEndpoint.Contact(resistor.Model.Id, resistor.Footprint.Contacts[0].Id);
        var board = mockup.Board!;
        var group = board.Definition.Holes.GroupBy(hole => hole.GroupId).First(holes => holes.Count() >= 2).Take(2).ToArray();
        var first = WireEndpoint.Hole(board.Id, group[0].Id);
        var second = WireEndpoint.Hole(board.Id, group[1].Id);
        project.Model.Circuit.PutWire(new(contact, first));
        project.Refresh();
        var changes = project.History.Count;

        mockup.IsNetInspectionActive = true;
        var point = WireGeometry.Resolve(project.Model, second)!.Value;
        mockup.InputCommand.Execute(new(CanvasAction.Down,
            PhysicalSettings.ToPixels(point.X) * mockup.Zoom + mockup.OffsetX,
            PhysicalSettings.ToPixels(point.Y) * mockup.Zoom + mockup.OffsetY));

        Assert.Same(project.Connectivity.Netlist.Find(contact), mockup.InspectedNet);
        Assert.NotNull(mockup.InspectedNet);
        Assert.Equal(changes, project.History.Count);
        mockup.InputCommand.Execute(new(CanvasAction.Cancel));
        Assert.Null(mockup.InspectedNet);
        Assert.False(mockup.IsNetInspectionActive);
    }

    [Fact]
    public void FocusCentersTheSelectedFootprintWithoutEditingIt()
    {
        using var design = new Stage4TestEnvironment();
        var project = design.Project;
        var mockup = design.Mockup;
        var board = project.Add(project.Library.Single(part => part.Id == "arduino-uno"))!;
        var placement = board.MockupPlacement;
        placement.MoveTo(135, 76);
        project.Refresh();
        var changes = project.History.Count;
        var pitch = mockup.Board?.Definition.HolePitch ?? PhysicalSettings.FreeGridMm;
        var bounds = ContactGeometry.Bounds(board.Footprint, placement, pitch);
        var center = new LayoutPoint(placement.X + bounds.X + bounds.Width / 2, placement.Y + bounds.Y + bounds.Height / 2);

        mockup.FocusSelectionCommand.Execute(null);

        Assert.Equal(mockup.ViewportWidth / 2, PhysicalSettings.ToPixels(center.X) * mockup.Zoom + mockup.OffsetX, 5);
        Assert.Equal(mockup.ViewportHeight / 2, PhysicalSettings.ToPixels(center.Y) * mockup.Zoom + mockup.OffsetY, 5);
        Assert.Equal(changes, project.History.Count);
    }
}
