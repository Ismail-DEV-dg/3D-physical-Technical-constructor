using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Scene;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.App.Controls;

internal static class WireDrawing
{
    private const double ViewportPaddingMm = 1.5;
    private static readonly Pen Outline = Ink(Brushes.Black, .85);
    private static readonly Pen Highlight = Ink(new SolidColorBrush(Color.FromArgb(100, 0, 229, 255)), 1.5);
    private static readonly Pen Handle = Ink(Brushes.DarkCyan, .16);
    private static readonly Pen Preview = Ink(Brushes.Cyan, .45);
    private static readonly ConcurrentDictionary<string, Pen> ColorPens = new();

    internal static void Draw(DrawingContext dc, BreadboardViewModel scene, bool adorners = true,
        SimulationState? simulation = null, bool showVoltage = false, bool showCurrent = false,
        WireDrawingCache? cache = null, LayoutRect? viewport = null)
    {
        cache ??= new();
        var budget = SimulationDrawing.MaximumParticles;
        var editor = scene.Project.Wiring;
        var selectedId = adorners ? editor.Selected?.Id : null;
        var hoveredId = adorners ? editor.HoveredId : null;
        var draggedStart = adorners ? editor.DraggedStart : null;
        var draggedEnd = adorners ? editor.DraggedEnd : null;
        foreach (var wire in scene.Project.Model.Circuit.Wires)
        {
            if (editor.GetPath(wire) is not { } path) continue;
            var selected = selectedId == wire.Id;
            if (adorners)
                path = path.WithEnds(selected && draggedStart is not null ? draggedStart : scene.ResolveDraftEndpoint(wire.Start),
                    selected && draggedEnd is not null ? draggedEnd : scene.ResolveDraftEndpoint(wire.End));
            var bounds = path.Bounds;
            if (viewport is { } view && !view.Intersects(new(bounds.X - ViewportPaddingMm, bounds.Y - ViewportPaddingMm,
                bounds.Width + 2 * ViewportPaddingMm, bounds.Height + 2 * ViewportPaddingMm))) continue;

            var geometry = cache.Get(path);
            if (selected || hoveredId == wire.Id) dc.DrawGeometry(null, Highlight, geometry);
            dc.DrawGeometry(null, Outline, geometry);
            var signal = simulation?.Wires.GetValueOrDefault(wire.Id);
            var color = showVoltage && signal?.Voltage is double volts ? SceneSignalStyle.VoltageColor(volts) : wire.Color;
            dc.DrawGeometry(null, ColorPens.GetOrAdd(color, static value => Ink(PhysicalDrawing.Ink(value), .55)), geometry);
            if (showCurrent && signal?.Current is double amps)
                budget -= SimulationDrawing.Flow(dc, path.Points, amps, simulation!.Microseconds, budget);
            if (selected)
                foreach (var p in path.Terminals) dc.DrawEllipse(Brushes.White, Handle, new Point(p.X, p.Y), .75, .75);
        }
        if (adorners && editor.PreviewStart is { } endpoint && scene.ResolveEndpoint(endpoint) is { } start)
            dc.DrawLine(Preview, new Point(start.X, start.Y), new Point(editor.Pointer.X, editor.Pointer.Y));
    }

    private static Pen Ink(Brush brush, double width)
    {
        var pen = new Pen(brush, width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        pen.Freeze();
        return pen;
    }
}
