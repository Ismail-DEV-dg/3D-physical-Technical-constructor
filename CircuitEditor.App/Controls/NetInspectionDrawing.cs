using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Wiring;

namespace CircuitEditor.App.Controls;

internal static class NetInspectionDrawing
{
    public static void Draw(DrawingContext context, BreadboardViewModel scene, LayoutRect viewport, WireDrawingCache cache, Brush ink)
    {
        if (scene.InspectedNet is not { } net) return;

        var netlist = scene.Project.Connectivity.Netlist;
        var trace = new Pen(ink, 1.05) { DashStyle = DashStyles.Dash };
        foreach (var wire in scene.Project.Model.Circuit.Wires)
        {
            if (netlist.Find(wire.Start) != net || scene.Project.Wiring.GetPath(wire) is not { } path)
                continue;

            var geometry = cache.Get(path);
            context.DrawGeometry(null, trace, geometry);
        }

        var pen = new Pen(ink, .3);
        foreach (var terminal in net.Terminals)
        {
            var endpoint = terminal.Kind switch
            {
                "c" => WireEndpoint.Contact(terminal.OwnerId, terminal.TerminalId),
                "h" => WireEndpoint.Hole(terminal.OwnerId, terminal.TerminalId),
                _ => null
            };
            if (endpoint is null || scene.ResolveEndpoint(endpoint) is not { } point ||
                point.X < viewport.X || point.X > viewport.Right || point.Y < viewport.Y || point.Y > viewport.Bottom)
                continue;

            context.DrawEllipse(null, pen, new Point(point.X, point.Y), .95, .95);
        }
    }
}
