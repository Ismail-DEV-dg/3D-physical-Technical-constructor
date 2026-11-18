using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Breadboards;
namespace CircuitEditor.App.ViewModels;
internal static class WorkspaceViewport
{
    private const double MarginPixels=30;
    internal static void Fit(MainViewModel vm)
    {
        var mock=vm.SelectedWorkspace.Kind==WorkspaceKind.Mockup;var points=new List<LayoutPoint>();
        void Rect(double x,double y,double width,double height){points.Add(new(x,y));points.Add(new(x+width,y+height));}
        foreach(var item in vm.Project.Components)
        {
            if(mock){var p=item.MockupPlacement;var b=ContactGeometry.Bounds(item.Footprint,p,PhysicalSettings.FreeGridMm);Rect(p.X+b.X,p.Y+b.Y,b.Width,b.Height);}
            else{var p=item.SchematicPlacement;var rotated=p.Rotation%180!=0;var w=rotated?item.Layout.Height:item.Layout.Width;var h=rotated?item.Layout.Width:item.Layout.Height;Rect(p.X+item.Layout.Width/2-w/2,p.Y+item.Layout.Height/2-h/2,w,h);}
        }
        if(mock)
        {
            foreach(var b in vm.Project.Model.Breadboards)
                foreach(var p in new[]{new LayoutPoint(0,0),new(b.Definition.Width,0),new(0,b.Definition.Height),new(b.Definition.Width,b.Definition.Height)})points.Add(BreadboardGeometry.World(b,p));
            points.AddRange(vm.Project.Model.Circuit.Wires.SelectMany(w=>CircuitEditor.Core.Wiring.WireCurve.Sample(vm.Project.Wiring.Points(w))));
        }
        if(points.Count==0)return;
        var scale=mock?PhysicalSettings.PixelsPerMillimeter:1;
        var left=points.Min(p=>p.X)*scale;var top=points.Min(p=>p.Y)*scale;var width=(points.Max(p=>p.X)-points.Min(p=>p.X))*scale;var height=(points.Max(p=>p.Y)-points.Min(p=>p.Y))*scale;
        var viewWidth=mock?vm.Mockup.ViewportWidth:vm.Schematic.ViewportWidth;var viewHeight=mock?vm.Mockup.ViewportHeight:vm.Schematic.ViewportHeight;
        var zoom=Math.Clamp(Math.Min((viewWidth-2*MarginPixels)/Math.Max(width,1),(viewHeight-2*MarginPixels)/Math.Max(height,1)),.25,3);
        var x=viewWidth/2-(left+width/2)*zoom;var y=viewHeight/2-(top+height/2)*zoom;
        if(mock){vm.Mockup.Zoom=zoom;vm.Mockup.OffsetX=x;vm.Mockup.OffsetY=y;vm.Mockup.Revision++;}
        else{vm.Schematic.Zoom=zoom;vm.Schematic.OffsetX=x;vm.Schematic.OffsetY=y;vm.Schematic.Revision++;}
    }
}
