using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Services;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Physical;
namespace CircuitEditor.App.Controls;
internal static class VoltageDrawing
{
    internal static void Draw(DrawingContext dc,ProjectViewModel project,bool mockup)
    {
        if(project.Analysis?.Results is not {ShowVoltages:true,IsStale:false,Result: { } result})return;
        var size=mockup ? 1.65 : 11;
        foreach(var net in project.Connectivity.Netlist.Nets)
        {
            var pin=net.Pins.FirstOrDefault();Point? point=null;
            if(pin is not null)
            {
                if(!mockup)point=SchematicConnectionsDrawing.Position(project,pin);
                else
                {
                    var item=project.Components.FirstOrDefault(c=>c.Model.Id==pin.ComponentId);
                    var contact=item?.Footprint.Contacts.FirstOrDefault(c=>c.ElectricalPinId==pin.PinId);
                    if(item is not null && contact is not null){var p=ContactGeometry.World(item.Footprint,contact,item.MockupPlacement,PhysicalSettings.FreeGridMm);point=new(p.X,p.Y);}
                }
            }
            if(point is { } position)Label(position,net.Name+": "+QuantityFormatter.Format(result.Voltages.GetValueOrDefault(net.Id),"В"));
        }
        if(mockup)
            foreach(var wire in project.Model.Circuit.Wires)
            {
                var terminal=project.Connectivity.Netlist.Nets.FirstOrDefault(n=>n.Terminals.Any(t=>t.OwnerId==wire.Start.OwnerId&&t.TerminalId==wire.Start.TerminalId&&t.Kind==(wire.Start.Kind==Core.Wiring.WireEndpointKind.ComponentContact?"c":"h")));
                var a=Core.Wiring.WireGeometry.Resolve(project.Model,wire.Start);var b=Core.Wiring.WireGeometry.Resolve(project.Model,wire.End);
                if(terminal is not null && a is { } start && b is { } end)Label(new((start.X+end.X)/2,(start.Y+end.Y)/2),QuantityFormatter.Format(result.Voltages.GetValueOrDefault(terminal.Id),"В"));
            }
        void Label(Point point,string text)
        {
            var formatted=WpfTextMeasurer.Format(text,size,Brushes.DarkCyan);
            var origin=new Point(point.X+size,point.Y-size*1.6);
            dc.DrawRectangle(Brushes.WhiteSmoke,null,new Rect(origin,new Size(formatted.WidthIncludingTrailingWhitespace,formatted.Height)));
            dc.DrawText(formatted,origin);
        }
    }
}
