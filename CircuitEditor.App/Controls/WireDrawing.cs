using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Scene;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Core.Wiring;

namespace CircuitEditor.App.Controls;

internal static class WireDrawing
{
    private const double ViewportPaddingMm = 1.5;
    private static readonly Pen Outline = Ink(PhysicalDrawing.Ink("#71807A"), .85);
    private static readonly Pen Highlight = Ink(new SolidColorBrush(Color.FromArgb(135, 112, 217, 238)), 1.5);
    private static readonly Pen Handle = Ink(PhysicalDrawing.Ink("#70D9EE"), .22);
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
            var baseColor = adorners && scene.ShowNetColors ? NetColor(scene, wire) : wire.Color;
            var color = showVoltage && signal?.Voltage is double volts ? SceneSignalStyle.VoltageColor(volts) : baseColor;
            dc.DrawGeometry(null, ColorPens.GetOrAdd(color, static value => Ink(PhysicalDrawing.Ink(value), .55)), geometry);
            if (showCurrent && signal?.Current is double amps)
                budget -= SimulationDrawing.Flow(dc, path.Points, amps, simulation!.Microseconds, budget);
            if (selected)
                foreach (var p in path.Terminals)
                    dc.DrawRectangle(PhysicalDrawing.Ink("#14171C"), Handle, new Rect(p.X - .6, p.Y - .6, 1.2, 1.2));
            if (adorners && scene.ShowWireLabels && !selected && hoveredId != wire.Id)
            {
                if (ControllerRole(scene, wire.Start) is not null) DrawTerminal(dc, wire.Start.TerminalId, path.Terminals[0]);
                if (ControllerRole(scene, wire.End) is not null) DrawTerminal(dc, wire.End.TerminalId, path.Terminals[^1]);
            }
        }
        if (adorners && editor.PreviewStart is { } endpoint && scene.ResolveEndpoint(endpoint) is { } start)
            dc.DrawLine(Preview, new Point(start.X, start.Y), new Point(editor.Pointer.X, editor.Pointer.Y));
    }

    private static PinType? ControllerRole(BreadboardViewModel scene, WireEndpoint endpoint)
    {
        var part = scene.Project.Components.FirstOrDefault(item => item.Model.Id == endpoint.OwnerId);
        return part?.Model.Definition.Category == "КОНТРОЛЛЕРЫ"
            ? WireGeometry.PinRole(scene.Project.Model, endpoint) : null;
    }

    private static string NetColor(BreadboardViewModel scene, Wire wire) =>
        (ControllerRole(scene, wire.Start), ControllerRole(scene, wire.End)) switch
        {
            (PinType.Ground, _) or (_, PinType.Ground) => "#A9B3B3",
            (PinType.Power, _) or (_, PinType.Power) => "#E9AC67",
            ({ }, _) or (_, { }) => "#57D9EC",
            _ => wire.Color
        };

    private static void DrawTerminal(DrawingContext dc, string terminal, LayoutPoint position)
    {
        const double size = 1.45;
        var text = Services.WpfTextMeasurer.Format(terminal, size, Brushes.WhiteSmoke);
        var box = new Rect(position.X + .8, position.Y - text.Height - 1.1, text.Width + 1.2, text.Height + .5);
        dc.DrawRoundedRectangle(PhysicalDrawing.Ink("#14171C"), null, box, .4, .4);
        dc.DrawText(text, new Point(box.X + .6, box.Y + .25));
    }

    private static Pen Ink(Brush brush, double width)
    {
        var pen = new Pen(brush, width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        pen.Freeze();
        return pen;
    }
}
