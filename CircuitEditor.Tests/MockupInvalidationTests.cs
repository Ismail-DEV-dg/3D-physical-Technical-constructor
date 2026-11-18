using CircuitEditor.App.Interaction;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.Tests;

public sealed class MockupInvalidationTests
{
    [Fact]
    public void HoverUpdatesCoordinatesWithoutInvalidatingDrawing()
    {
        using var project = TestProjectFactory.Create();
        using var scene = TestProjectFactory.Mockup(project);
        project.Add(project.Library.Single(item => item.Id == "lcd1602"));
        var before = scene.Revision;
        for (var i = 0; i < 100; i++) scene.InputCommand.Execute(new CanvasInput(CanvasAction.Move, 100 + i, 200));
        Assert.Equal(before, scene.Revision);
        Assert.Equal(PhysicalSettings.ToMillimeters((199 - scene.OffsetX) / scene.Zoom), scene.CursorX);
        Assert.Equal(PhysicalSettings.ToMillimeters((200 - scene.OffsetY) / scene.Zoom), scene.CursorY);
    }

    [Fact]
    public void StatusDoesNotInvalidateButModelChangeDoes()
    {
        using var project = TestProjectFactory.Create();
        using var scene = TestProjectFactory.Mockup(project);
        var before = scene.Revision;
        project.StatusMessage = "Проверка";
        project.PropertyError = "Ошибка ввода";
        Assert.Equal(before, scene.Revision);
        project.Add(project.Library.Single(item => item.Id == "lcd1602"));
        Assert.True(scene.Revision > before);
        before = scene.Revision;
        project.Refresh();
        Assert.True(scene.Revision > before);
    }

    [Fact]
    public void DragPanAndZoomStillInvalidateAndKeepGesturesWorking()
    {
        using var project = TestProjectFactory.Create();
        using var scene = TestProjectFactory.Mockup(project);
        var item = project.Add(project.Library.Single(item => item.Id == "lcd1602"))!;
        item.MockupPlacement.MoveTo(10, 20);
        var x = PhysicalSettings.ToPixels(15) + scene.OffsetX;
        var y = PhysicalSettings.ToPixels(30) + scene.OffsetY;
        scene.InputCommand.Execute(new CanvasInput(CanvasAction.Down, x, y));
        var before = scene.Revision;
        scene.InputCommand.Execute(new CanvasInput(CanvasAction.Move, x + 12, y + 12));
        Assert.True(scene.Revision > before);
        Assert.NotEqual(item.MockupPlacement.X, scene.PlacementOf(item).X);
        scene.InputCommand.Execute(new CanvasInput(CanvasAction.Up, x + 12, y + 12));
        Assert.Equal(scene.PlacementOf(item).X, item.MockupPlacement.X);
        var offset = scene.OffsetX;
        scene.InputCommand.Execute(new CanvasInput(CanvasAction.Down, 100, 100, Pan: true));
        before = scene.Revision;
        scene.InputCommand.Execute(new CanvasInput(CanvasAction.Move, 140, 120));
        Assert.True(scene.Revision > before); Assert.Equal(offset + 40, scene.OffsetX);
        scene.InputCommand.Execute(new CanvasInput(CanvasAction.Up, 140, 120));
        before = scene.Revision;
        scene.InputCommand.Execute(new CanvasInput(CanvasAction.Wheel, 140, 120, Delta: 120));
        Assert.True(scene.Revision > before); Assert.True(scene.Zoom > 1);
    }
}
