using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Core.Wiring;
namespace CircuitEditor.App.Controls;
internal static class WireDrawing
{
    internal static void Draw(DrawingContext dc, BreadboardViewModel scene, bool adorners = true,
        SimulationState? simulation = null, bool showVoltage = false, bool showCurrent = false)
    {
        var budget = SimulationDrawing.MaximumParticles;
        var editor = scene.Project.Wiring;
        foreach (var wire in scene.Project.Model.Circuit.Wires)
        {
            var terminals = editor.Points(wire).ToArray();
            if (terminals.Length < 2) continue;
            if (adorners) { terminals[0] = scene.ResolveEndpoint(wire.Start) ?? terminals[0]; terminals[^1] = scene.ResolveEndpoint(wire.End) ?? terminals[^1]; }
            if (adorners && editor.Selected?.Id == wire.Id)
            { terminals[0] = editor.DraggedStart ?? terminals[0]; terminals[^1] = editor.DraggedEnd ?? terminals[^1]; }
            var points = WireCurve.Sample(terminals);
            var selected = adorners && editor.Selected?.Id == wire.Id;
            var hovered = adorners && editor.HoveredId == wire.Id;
            if (selected || hovered) Line(dc, points, new SolidColorBrush(Color.FromArgb(100,0,229,255)), 1.5);
            Line(dc, points, Brushes.Black, .85);
            var signal = simulation?.Wires.GetValueOrDefault(wire.Id);
            var brush = showVoltage && signal?.Voltage is double volts ? SimulationDrawing.VoltageBrush(volts)
                : PhysicalDrawing.Ink(wire.Color);
            Line(dc, points, brush, .55);
            if (showCurrent && signal?.Current is double amps)
                budget -= SimulationDrawing.Flow(dc, points, amps, simulation!.Microseconds, budget);
            if (selected)
                foreach (var p in terminals) dc.DrawEllipse(Brushes.White, new Pen(Brushes.DarkCyan,.16), new Point(p.X,p.Y),.75,.75);
        }
        if (adorners && editor.PreviewStart is { } endpoint && WireGeometry.Resolve(scene.Project.Model, endpoint) is { } start)
            Line(dc, new[] { start, editor.Pointer }, Brushes.Cyan, .45);
    }
    private static void Line(DrawingContext dc, IReadOnlyList<LayoutPoint> points, Brush brush, double width)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(points[0].X,points[0].Y),false,false);
            context.PolyLineTo(points.Skip(1).Select(p => new Point(p.X,p.Y)).ToArray(),true,false);
        }
        geometry.Freeze(); dc.DrawGeometry(null, new Pen(brush,width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, geometry);
    }
}
