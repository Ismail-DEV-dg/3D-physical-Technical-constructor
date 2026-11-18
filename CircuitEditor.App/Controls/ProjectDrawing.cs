using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Wiring;
namespace CircuitEditor.App.Controls;
internal static class ProjectDrawing
{
    internal static DrawingGroup Create(BreadboardViewModel mockup, SchematicViewModel schematic, WorkspaceKind kind)
    {
        var drawing = new DrawingGroup();
        using var dc = drawing.Open();
        if (kind == WorkspaceKind.Schematic)
        {
            var surface = new SchematicSurface();
            SchematicConnectionsDrawing.Draw(dc,schematic.Project);
            foreach (var c in schematic.Components) surface.DrawComponent(dc,c,false);
        }
        else
        {
            dc.PushTransform(new ScaleTransform(PhysicalSettings.PixelsPerMillimeter,PhysicalSettings.PixelsPerMillimeter));
            foreach (var board in mockup.Project.Model.Breadboards)
            {
                dc.PushTransform(new TranslateTransform(board.X,board.Y)); BreadboardDrawing.DrawBase(dc,board.Definition,false); dc.Pop();
            }
            var cache = new ComponentDrawingCache();
            foreach (var c in mockup.Project.Components)
            {
                var p = c.MockupPlacement; dc.PushTransform(new TranslateTransform(p.X,p.Y)); dc.PushTransform(new RotateTransform(p.Rotation));
                dc.DrawDrawing(cache.Get(c,p,PhysicalSettings.FreeGridMm));
                if (c.Footprint.Renderer == "potentiometer") PotentiometerDrawing.DrawKnob(dc,c);
                dc.Pop(); dc.Pop();
            }
            WireDrawing.Draw(dc,mockup,false);
            dc.Pop();
        }
        return drawing;
    }
}
