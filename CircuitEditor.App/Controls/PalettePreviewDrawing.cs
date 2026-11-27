using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Services;

namespace CircuitEditor.App.Controls;

internal static class PalettePreviewDrawing
{
    private static readonly Pen Accepted = PhysicalDrawing.Frozen(new Pen(PhysicalDrawing.Ink("#7DF1BB"), .33));
    private static readonly Pen Rejected = PhysicalDrawing.Frozen(new Pen(PhysicalDrawing.Ink("#FF7580"), .40));
    private static readonly Brush Fill = PhysicalDrawing.Frozen(PhysicalDrawing.Ink("#7069B5A5"));
    private static readonly Brush Hole = PhysicalDrawing.Frozen(PhysicalDrawing.Ink("#C026594B"));

    internal static void Draw(DrawingContext dc, PalettePlacementPreview preview, BreadboardInstance? board, double pitch)
    {
        var pose = preview.Pose;
        var outline = preview.Insertion is { Success: false } ? Rejected : Accepted;
        var body = ContactGeometry.Body(preview.Footprint, pose, pitch);
        dc.PushTransform(new TranslateTransform(pose.X, pose.Y));
        dc.PushTransform(new RotateTransform(pose.Rotation));
        dc.DrawRoundedRectangle(preview.Insertion is { Success: false } ? null : Fill, outline,
            PhysicalDrawing.Rect(body), .8, .8);
        dc.Pop();
        dc.Pop();
        DrawHoles(dc, board, preview.Insertion);
    }

    internal static void DrawHoles(DrawingContext dc, BreadboardInstance? board, InsertionResult? preview)
    {
        if (board is null || preview is not { Success: true }) return;
        foreach (var binding in preview.Bindings)
        {
            var pin = board.Definition.Holes.First(hole => hole.Id == binding.HoleId);
            var position = BreadboardGeometry.World(board, pin.Position);
            dc.DrawEllipse(Hole, Accepted, new Point(position.X, position.Y), .88, .88);
        }
    }

    internal static void DrawMovingBoard(DrawingContext dc, BreadboardInstance board)
    {
        dc.DrawRoundedRectangle(Fill, Accepted, new Rect(0, 0, board.Definition.Width, board.Definition.Height), 1.4, 1.4);
    }
}
