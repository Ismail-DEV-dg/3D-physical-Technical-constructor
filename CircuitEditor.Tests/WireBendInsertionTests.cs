using CircuitEditor.App.Interaction;
using CircuitEditor.Core.History;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Wiring;

namespace CircuitEditor.Tests;

public sealed class WireBendInsertionTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void InsertionUsesClickedCurvedSpanInEitherDirection(int span, bool reverse)
    {
        LayoutPoint[] points = [new(0, 0), new(20, 40), new(60, -20), new(100, 0)];
        if (reverse) Array.Reverse(points);
        var pointer = WireCurve.Sample(points)[span * WireCurve.SegmentsPerSpan + WireCurve.SegmentsPerSpan / 2];

        Assert.Equal(span, WireCurve.FindInsertionIndex(points, pointer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void NewBendStaysOnClickedSpanAndWholeDragIsOneUndoStep(int span)
    {
        using var e = new Stage4TestEnvironment();
        var p = e.Project; var editor = p.Wiring; var board = e.Mockup.Board!;
        var wire = new Wire(Guid.NewGuid(),
            WireEndpoint.Hole(board.Id, board.Definition.Holes.First().Id),
            WireEndpoint.Hole(board.Id, board.Definition.Holes.Last().Id),
            WirePalette.Colors[0], [new(150, 80), new(210, 80)]);
        p.Model.Circuit.PutWire(wire); p.Refresh(); p.History.Clear();
        var initial = new ProjectSnapshot(p.Model);
        var pointer = WireCurve.Sample(editor.Points(wire))[
            span * WireCurve.SegmentsPerSpan + WireCurve.SegmentsPerSpan / 2];
        editor.Handle(new(CanvasAction.Down), pointer); // Select the wire.
        editor.Handle(new(CanvasAction.Down), pointer); // Insert on the selected span.

        Assert.Equal(pointer, editor.Selected!.ControlPoints[span]);
        Assert.Equal(wire.ControlPoints, editor.Selected.ControlPoints.Where((_, i) => i != span));
        var moved = new LayoutPoint(pointer.X - 4, pointer.Y + 3);
        editor.Handle(new(CanvasAction.Move), moved);
        editor.Handle(new(CanvasAction.Up), moved);
        Assert.Equal(moved, editor.Selected.ControlPoints[span]);
        Assert.Equal(wire.Start, editor.Selected.Start);
        Assert.Equal(wire.End, editor.Selected.End);
        Assert.Equal(wire.Color, editor.Selected.Color);
        Assert.Equal(1, p.History.Count);
        var final = new ProjectSnapshot(p.Model);
        p.UndoCommand.Execute(null); Assert.True(initial.Matches(p.Model));
        p.RedoCommand.Execute(null); Assert.True(final.Matches(p.Model));
    }

    [Fact]
    public void EscapeCancelsInsertedBendWithoutHistory()
    {
        using var e = new Stage4TestEnvironment();
        var p = e.Project; var editor = p.Wiring; var board = e.Mockup.Board!;
        var wire = new Wire(Guid.NewGuid(),
            WireEndpoint.Hole(board.Id, board.Definition.Holes.First().Id),
            WireEndpoint.Hole(board.Id, board.Definition.Holes.Last().Id),
            WirePalette.Colors[0], [new(150, 80)]);
        p.Model.Circuit.PutWire(wire); p.Refresh(); p.History.Clear();
        var before = new ProjectSnapshot(p.Model);
        var pointer = WireCurve.Sample(editor.Points(wire))[WireCurve.SegmentsPerSpan / 2];
        editor.Handle(new(CanvasAction.Down), pointer);
        editor.Handle(new(CanvasAction.Down), pointer);
        editor.Handle(new(CanvasAction.Move), new(pointer.X - 5, pointer.Y));
        editor.Handle(new(CanvasAction.Cancel), pointer);

        Assert.True(before.Matches(p.Model));
        Assert.Equal(0, p.History.Count);
    }
}
