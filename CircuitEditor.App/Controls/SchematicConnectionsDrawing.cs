using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Models;
namespace CircuitEditor.App.Controls;
internal static class SchematicConnectionsDrawing
{
    internal static Point? Position(ProjectViewModel project, PinReference pin)
    {
        var item=project.Components.FirstOrDefault(c=>c.Model.Id==pin.ComponentId);
        if(item is null)return null;
        var point=Core.Layout.SchematicPinGeometry.Position(item.Layout,item.SchematicPlacement,pin.PinId);
        return point is { } p ? new Point(p.X,p.Y) : null;
    }
    internal static void Draw(DrawingContext dc, ProjectViewModel project)
    {
        var pen=new Pen(Brushes.DarkCyan,1.5);
        foreach(var net in project.Connectivity.Netlist.Nets.Where(n=>n.Pins.Count>1))
        {
            var points=net.Pins.Select(p=>Position(project,p)).Where(p=>p is not null).Select(p=>p!.Value).ToArray();
            for(var i=1;i<points.Length;i++)
            {
                var a=points[i-1];var b=points[i];var corner=new Point(b.X,a.Y);
                dc.DrawLine(pen,a,corner);dc.DrawLine(pen,corner,b);
                dc.DrawEllipse(Brushes.DarkCyan,null,b,3,3);
            }
            if(points.Length>0 && (net.Name.StartsWith("GND",StringComparison.Ordinal) || net.Name.StartsWith("5V",StringComparison.Ordinal) || net.Name.StartsWith("3V3",StringComparison.Ordinal)))
                dc.DrawText(WpfTextMeasurer.Format(net.Name,12,Brushes.DarkCyan),new Point(points[0].X+6,points[0].Y-18));
        }
    }
}
