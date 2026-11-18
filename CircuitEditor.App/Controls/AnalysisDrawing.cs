using System.Windows;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Validation;
namespace CircuitEditor.App.Controls;
internal static class AnalysisDrawing
{
    internal static void Draw(DrawingContext dc,ProjectViewModel project,bool mockup)
    {
        if(project.Analysis is not { } analysis)return;
        foreach(var item in project.Components)
            foreach(var pin in item.Model.Definition.Pins.Where(p=>p.IsExposed && !p.IsNoConnect))
            {
                if(analysis.Severity(new(item.Model.Id,pin.Id)) is not { } severity)continue;
                var brush=severity==ErcSeverity.Error ? Brushes.Crimson : Brushes.Goldenrod;
                if(mockup)
                {
                    foreach(var contact in item.Footprint.Contacts.Where(c=>c.ElectricalPinId==pin.Id))
                    {
                        var p=ContactGeometry.World(item.Footprint,contact,item.MockupPlacement,PhysicalSettings.FreeGridMm);
                        dc.DrawEllipse(null,new Pen(brush,.3),new Point(p.X,p.Y),1,1);
                    }
                }
                else if(SchematicConnectionsDrawing.Position(project,new(item.Model.Id,pin.Id)) is { } p)
                    dc.DrawEllipse(null,new Pen(brush,2),p,6,6);
            }
    }
}
