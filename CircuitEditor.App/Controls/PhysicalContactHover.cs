using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Breadboards;
namespace CircuitEditor.App.Controls;
internal static class PhysicalContactHover
{
    private const double ContactRadiusMm=.9;
    private const double HoleRadiusMm=.8;
    internal static (string Key,LayoutPoint Position)? Find(BreadboardViewModel scene,LayoutPoint point)
    {
        static double Distance(LayoutPoint a,LayoutPoint b)=>Math.Pow(a.X-b.X,2)+Math.Pow(a.Y-b.Y,2);
        foreach(var item in scene.Project.Components.Reverse())
        {
            if(!scene.Hit(item,point.X,point.Y))continue;
            foreach(var contact in item.Footprint.Contacts)
            {
                var p=ContactGeometry.World(item.Footprint,contact,scene.PlacementOf(item),PhysicalSettings.FreeGridMm);
                if(Distance(point,p)<=ContactRadiusMm*ContactRadiusMm)return ($"{item.Model.Id}:{contact.Id}",p);
            }
            return null;
        }
        if(scene.Board is { } board)
            foreach(var hole in board.Definition.Holes)
            {
                var p=BreadboardGeometry.World(board,hole.Position);
                if(Distance(point,p)<=HoleRadiusMm*HoleRadiusMm)return ($"{board.Id}:{hole.Id}",p);
            }
        return null;
    }
}
