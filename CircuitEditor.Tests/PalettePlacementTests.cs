using CircuitEditor.App.Interaction;
using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.Tests;

public sealed class PalettePlacementTests
{
    [Fact]
    public void ExplicitRotationUsesTheSameHolesForNewAndExistingPotentiometer()
    {
        using var environment = new Stage4TestEnvironment();
        var project = environment.Project;
        var definition = project.Library.Single(item => item.Id == "potentiometer");
        var footprint = project.Model.Footprints.Single(item => item.ComponentDefinitionId == definition.Id);
        var board = environment.Mockup.Board!;
        var hole = board.Definition.Holes.Single(candidate => candidate.Id == "left/B/11");
        var point = BreadboardGeometry.World(board, hole.Position);
        var insertion = new Core.Services.ComponentInsertionService();
        var fromPalette = insertion.PreviewNew(project.Model, definition, footprint, board.Id, point, 0);
        var part = project.Add(definition)!;
        var fromCanvas = insertion.Preview(project.Model, part.Model.Id, footprint, board.Id, new(point.X, point.Y));
        Assert.True(fromCanvas.Success, fromCanvas.Error);
        Assert.True(fromPalette.Success, fromPalette.Error);
        Assert.Equal(fromCanvas.Bindings.Select(binding => binding.HoleId), fromPalette.Bindings.Select(binding => binding.HoleId));
        Assert.Equal(hole.Id, fromPalette.Bindings.First().HoleId);
    }

    [Fact]
    public void ArmedPlacementCanRotateBeforeTheFirstClick()
    {
        using var environment = new Stage4TestEnvironment();
        var mockup = environment.Mockup;
        mockup.ArmQuickPlacementCommand.Execute("resistor");
        var move = new CanvasInput(CanvasAction.Move, mockup.OffsetX + 120, mockup.OffsetY + 120);
        mockup.InputCommand.Execute(move);
        mockup.InputCommand.Execute(new CanvasInput(CanvasAction.Rotate));
        Assert.Empty(environment.Project.Components);
        Assert.Equal(90, mockup.PalettePreview!.Pose.Rotation);
        mockup.InputCommand.Execute(move with { Action = CanvasAction.Down });
        Assert.Equal(90, Assert.Single(environment.Project.Components).MockupPlacement.Rotation);
    }

    [Fact]
    public void DragRotationStartsAtTheAngleShownByAutomaticPreview()
    {
        var payload = new PaletteDragPayload("resistor") { CurrentRotation = () => 90 };
        var refreshed = false;
        payload.RefreshTarget = _ => refreshed = true;
        payload.Rotate();
        Assert.True(refreshed);
        Assert.Equal(180, payload.Rotation);
    }

    [Fact]
    public void MovingTheBoardKeepsTheHighlightActiveUntilMouseUp()
    {
        using var environment = new Stage4TestEnvironment();
        var mockup = environment.Mockup;
        var board = mockup.Board!;
        CanvasInput At(CanvasAction action, double x, double y) => new(action,
            mockup.OffsetX + PhysicalSettings.ToPixels(x) * mockup.Zoom,
            mockup.OffsetY + PhysicalSettings.ToPixels(y) * mockup.Zoom);
        mockup.InputCommand.Execute(At(CanvasAction.Down, board.X + .5, board.Y + .5));
        Assert.True(mockup.BoardInteraction.IsDragging);
        mockup.InputCommand.Execute(At(CanvasAction.Move, board.X + 5, board.Y + 5));
        mockup.InputCommand.Execute(new CanvasInput(CanvasAction.Rotate));
        Assert.Equal(90, mockup.Board!.Rotation);
        Assert.True(mockup.BoardInteraction.IsDragging);
        mockup.InputCommand.Execute(At(CanvasAction.Up, board.X + 5, board.Y + 5));
        Assert.False(mockup.BoardInteraction.IsDragging);
        Assert.Equal(1, environment.Project.History.Count);
    }

    [Fact]
    public void HeldResistorCanRotateBeforePlacementAndHighlightsItsHoles()
    {
        using var environment = new Stage4TestEnvironment();
        var mockup = environment.Mockup;
        var project = environment.Project;
        var item = project.Add(project.Library.Single(definition => definition.Id == "resistor"))!;
        var origin = item.MockupPlacement.Copy();
        var history = project.History.Count;
        var board = mockup.Board!;
        var hole = board.Definition.Holes.Single(candidate => candidate.Id == "left/A/11");
        var target = BreadboardGeometry.World(board, hole.Position);
        CanvasInput At(CanvasAction action, double x, double y) => new(action,
            mockup.OffsetX + PhysicalSettings.ToPixels(x) * mockup.Zoom,
            mockup.OffsetY + PhysicalSettings.ToPixels(y) * mockup.Zoom);
        mockup.InputCommand.Execute(At(CanvasAction.Down, origin.X, origin.Y));
        Assert.True(mockup.CanRotateDuringDrag);
        mockup.InputCommand.Execute(new CanvasInput(CanvasAction.Rotate));
        Assert.Equal(90, mockup.PlacementOf(item).Rotation);
        Assert.Equal(0, item.MockupPlacement.Rotation);
        mockup.InputCommand.Execute(At(CanvasAction.Move, target.X, target.Y));
        var preview = mockup.HeldPlacementPreview?.Insertion;
        Assert.NotNull(preview);
        Assert.True(preview.Success, preview.Error);
        Assert.Equal(2, preview.Bindings.Count);
        mockup.InputCommand.Execute(At(CanvasAction.Up, target.X, target.Y));
        Assert.Null(mockup.HeldPlacementPreview);
        Assert.Equal(90, item.MockupPlacement.Rotation);
        Assert.Equal(preview.Bindings.ToArray(), project.Model.Bindings.ToArray());
        Assert.Equal(history + 1, project.History.Count);
    }

    [Fact]
    public void DroppingWhereThePinsCannotFitDoesNotCreateAComponent()
    {
        using var environment = new Stage4TestEnvironment();
        var mockup = environment.Mockup;
        var board = mockup.Board!;
        var request = new CanvasInput(CanvasAction.DropPreview,
            mockup.OffsetX + PhysicalSettings.ToPixels(board.X + .1) * mockup.Zoom,
            mockup.OffsetY + PhysicalSettings.ToPixels(board.Y + .1) * mockup.Zoom, DefinitionId: "hc-sr04");
        mockup.InputCommand.Execute(request);
        Assert.False(mockup.PalettePreview!.Insertion!.Success);
        mockup.InputCommand.Execute(request with { Action = CanvasAction.Drop });
        Assert.Empty(environment.Project.Components);
        Assert.Equal(0, environment.Project.History.Count);
        Assert.Null(mockup.PalettePreview);
    }

    [Theory]
    [InlineData("resistor", 2)]
    [InlineData("led", 2)]
    [InlineData("button", 4)]
    [InlineData("hc-sr04", 4)]
    public void DragFromLibraryPreviewsAllHolesAndCommitsExactlyThatPlacement(string id, int pins)
    {
        using var environment = new Stage4TestEnvironment();
        var mockup = environment.Mockup;
        var board = mockup.Board!;
        var hole = board.Definition.Holes.Single(item => item.Id == "left/A/11");
        var anchor = BreadboardGeometry.World(board, hole.Position);
        var request = new CanvasInput(CanvasAction.DropPreview, mockup.OffsetX + PhysicalSettings.ToPixels(anchor.X) * mockup.Zoom,
            mockup.OffsetY + PhysicalSettings.ToPixels(anchor.Y) * mockup.Zoom, DefinitionId: id);

        mockup.InputCommand.Execute(request);
        Assert.Empty(environment.Project.Components);
        Assert.NotNull(mockup.PalettePreview);
        var fit = mockup.PalettePreview!.Insertion!;
        Assert.True(fit.Success, fit.Error);
        Assert.Equal(pins, fit.Bindings.Count);
        var expected = fit.Bindings.Select(binding => binding.HoleId).ToArray();
        mockup.InputCommand.Execute(request with { Action = CanvasAction.DropLeave });
        Assert.Null(mockup.PalettePreview);
        mockup.InputCommand.Execute(request with { Action = CanvasAction.Drop });
        var instance = Assert.Single(environment.Project.Components);
        Assert.Equal(expected, environment.Project.Model.Bindings.Select(binding => binding.HoleId));
        Assert.Equal(fit.Placement!.Rotation, instance.MockupPlacement.Rotation);
        Assert.Equal(1, environment.Project.History.Count);
    }
}
