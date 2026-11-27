using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Breadboards;

namespace CircuitEditor.App.Controls;

internal sealed class BreadboardDrawingCache
{
    private BreadboardDefinition? definition;
    private readonly Dictionary<bool, DrawingGroup> bases = [];
    private Dictionary<string, BreadboardHole> holes = [];
    private static readonly Pen Occupied = FrozenPen("#FF3B5C");

    public void Draw(DrawingContext dc, BreadboardInstance board, BreadboardViewModel scene)
    {
        if (!ReferenceEquals(definition, board.Definition))
        {
            Clear(); definition = board.Definition;
            holes = definition.Holes.ToDictionary(hole => hole.Id);
        }
        var available = scene.ShowAvailableHoles;
        if (!bases.TryGetValue(available, out var drawing))
        {
            drawing = new DrawingGroup();
            using (var context = drawing.Open()) BreadboardDrawing.DrawBase(context, board.Definition, available);
            drawing.Freeze(); bases.Add(available, drawing);
        }
        dc.PushTransform(new TranslateTransform(board.X, board.Y));
        dc.PushTransform(new RotateTransform(board.Rotation));
        dc.DrawDrawing(drawing);
        if (scene.BoardInteraction.IsDragging) PalettePreviewDrawing.DrawMovingBoard(dc, board);
        else if(scene.BoardInteraction.Selected)dc.DrawRoundedRectangle(null,new Pen(Brushes.DarkCyan,.5),new Rect(0,0,board.Definition.Width,board.Definition.Height),1.4,1.4);
        foreach (var binding in scene.Project.Model.Bindings)
            if (binding.BoardId == board.Id) DrawRing(dc, binding.HoleId, Occupied);
        dc.Pop(); dc.Pop();
    }

    private void DrawRing(DrawingContext dc, string id, Pen pen)
    {
        if (holes.TryGetValue(id, out var hole))
            dc.DrawEllipse(null, pen, new Point(hole.Position.X, hole.Position.Y), .65, .65);
    }

    public void Clear() { definition = null; bases.Clear(); holes.Clear(); }

    private static Pen FrozenPen(string color)
    {
        var pen = new Pen(PhysicalDrawing.Ink(color), .17);
        pen.Freeze();
        return pen;
    }
}
