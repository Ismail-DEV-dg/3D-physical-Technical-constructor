using CircuitEditor.Infrastructure.Repositories;
using CircuitEditor.App.Interaction;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Geometry;

namespace CircuitEditor.Tests;

public sealed class SchematicViewModelTests
{
    private static SchematicViewModel Create() => TestProjectFactory.Schematic();

    [Fact]
    public void DropConvertsViewportPositionSnapsAndSelectsNewComponent()
    {
        var model = Create();
        model.InputCommand.Execute(new CanvasInput(CanvasAction.Drop, 221, 181, DefinitionId: "resistor"));
        var component = Assert.Single(model.Components);
        Assert.Equal(DiagramGeometry.Snap(221 - model.OffsetX - component.Layout.Width / 2), component.SchematicPlacement.X);
        Assert.Equal(DiagramGeometry.Snap(181 - model.OffsetY - component.Layout.Height / 2), component.SchematicPlacement.Y);
        Assert.True(component.IsSelected);
        Assert.Equal("R1", component.Reference);
    }

    [Fact]
    public void DragMovesGroupOnGridEscapeRestoresAndDeleteRemovesSelection()
    {
        var model = Create();
        var definition = model.Library.Single(item => item.Id == "resistor");
        var first = model.AddComponent(definition, 100, 100);
        var second = model.AddComponent(definition, 300, 100);
        model.InputCommand.Execute(new CanvasInput(CanvasAction.SelectAll));
        model.InputCommand.Execute(new CanvasInput(CanvasAction.Down, 220, 190));
        model.InputCommand.Execute(new CanvasInput(CanvasAction.Move, 252, 202));
        Assert.Equal(140, first.SchematicPlacement.X);
        Assert.Equal(340, second.SchematicPlacement.X);
        model.InputCommand.Execute(new CanvasInput(CanvasAction.Cancel));
        Assert.Equal(100, first.SchematicPlacement.X);
        Assert.Equal(300, second.SchematicPlacement.X);
        model.RotateSelectedCommand.Execute(null);
        Assert.All(model.Components, item => Assert.Equal(90, item.SchematicPlacement.Rotation));
        model.DeleteSelectedCommand.Execute(null);
        Assert.Empty(model.Components);
    }

    [Fact]
    public void MarqueeSelectsIntersectingComponentsAndCtrlAddsToSelection()
    {
        var model = Create();
        var definition = model.Library.Single(item => item.Id == "resistor");
        var first = model.AddComponent(definition, 100, 100);
        var second = model.AddComponent(definition, 400, 100);
        model.InputCommand.Execute(new CanvasInput(CanvasAction.Down, 120, 120));
        model.InputCommand.Execute(new CanvasInput(CanvasAction.Move, 310, 260));
        model.InputCommand.Execute(new CanvasInput(CanvasAction.Up, 310, 260));
        Assert.True(first.IsSelected);
        Assert.False(second.IsSelected);
        model.InputCommand.Execute(new CanvasInput(CanvasAction.Down, 460, 200, Control: true));
        model.InputCommand.Execute(new CanvasInput(CanvasAction.Up, 460, 200, Control: true));
        Assert.Equal(2, model.SelectedCount);
    }

    [Fact]
    public void ZoomKeepsWorldPointUnderCursorAndPanDoesNotMoveComponents()
    {
        var model = Create();
        var item = model.AddComponent(model.Library.First(), 100, 100);
        const double x = 300, y = 250;
        var worldX = (x - model.OffsetX) / model.Zoom;
        var worldY = (y - model.OffsetY) / model.Zoom;
        model.InputCommand.Execute(new CanvasInput(CanvasAction.Wheel, x, y, Delta: 120));
        Assert.Equal(worldX, (x - model.OffsetX) / model.Zoom, 8);
        Assert.Equal(worldY, (y - model.OffsetY) / model.Zoom, 8);
        model.InputCommand.Execute(new CanvasInput(CanvasAction.Down, x, y, Pan: true));
        var previousOffset = model.OffsetX;
        model.InputCommand.Execute(new CanvasInput(CanvasAction.Move, x + 50, y + 30));
        model.InputCommand.Execute(new CanvasInput(CanvasAction.Up, x + 50, y + 30));
        Assert.Equal(previousOffset + 50, model.OffsetX, 8);
        Assert.Equal(100, item.SchematicPlacement.X);
        model.InputCommand.Execute(new CanvasInput(CanvasAction.Wheel, x, y, Delta: int.MaxValue));
        Assert.Equal(SchematicViewModel.MaximumZoom, model.Zoom);
        model.InputCommand.Execute(new CanvasInput(CanvasAction.Wheel, x, y, Delta: int.MinValue));
        Assert.Equal(SchematicViewModel.MinimumZoom, model.Zoom);
    }

    [Fact]
    public void PropertyValidationDoesNotCommitInvalidDraft()
    {
        var model = Create();
        var selected = model.AddComponent(model.Library.Single(item => item.Id == "lcd1602-i2c"), 0, 0);
        selected.EditI2cAddress = "0x80";
        model.ApplyPropertiesCommand.Execute(null);
        Assert.NotEmpty(model.PropertyError);
        Assert.Equal("0x27", selected.Model.I2cAddress);
        selected.EditI2cAddress = "0x3f";
        selected.EditColor = "#39FF88";
        model.ApplyPropertiesCommand.Execute(null);
        Assert.Empty(model.PropertyError);
        Assert.Equal("0x3F", selected.Model.I2cAddress);
        Assert.Equal("#39FF88", selected.Model.Color);
    }
}
