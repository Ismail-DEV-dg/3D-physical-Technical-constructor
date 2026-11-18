using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CircuitEditor.App.Controls;
using CircuitEditor.App.Services;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Breadboards;
namespace CircuitEditor.Tests;
internal static class UnoAndHoverPresentationChecks
{
    internal static void Run(ResourceDictionary resources)
    {
        using var project=TestProjectFactory.Create(new WpfTextMeasurer());using var scene=TestProjectFactory.Mockup(project);
        var uno=project.Add(project.Library.Single(d=>d.Id=="arduino-uno"))!;uno.MockupPlacement.MoveTo(5,5);
        var surface=new BreadboardSurface{Scene=scene,Resources=resources};surface.Measure(new Size(1000,700));surface.Arrange(new Rect(0,0,1000,700));surface.UpdateLayout();
        var pin=uno.Footprint.Contacts.Single(c=>c.Id=="5V");var pos=ContactGeometry.World(uno.Footprint,pin,uno.MockupPlacement,PhysicalSettings.FreeGridMm);
        var viewport=new Point(PhysicalSettings.ToPixels(pos.X)*scene.Zoom+scene.OffsetX,PhysicalSettings.ToPixels(pos.Y)*scene.Zoom+scene.OffsetY);
        var before=project.Revision;surface.UpdateContactHover(viewport);Assert.Equal($"{uno.Model.Id}:5V",surface.HoveredContact);Assert.Equal(before,project.Revision);
        surface.UpdateContactHover(new Point(-100,-100));Assert.Null(surface.HoveredContact);
        project.Model.TransformBoard(scene.Board!.Id,120,20,90);var board=scene.Board!;
        var hole=board.Definition.Holes.Single(h=>h.Id=="left/B/02");var hpoint=BreadboardGeometry.World(board,hole.Position);
        Assert.Equal($"{board.Id}:{hole.Id}",PhysicalContactHover.Find(scene,hpoint)!.Value.Key);
        var cache=new ComponentDrawingCache();var drawing=cache.Get(uno,uno.MockupPlacement,PhysicalSettings.FreeGridMm);Assert.True(drawing.IsFrozen);
        Assert.Same(drawing,cache.Get(uno,uno.MockupPlacement,PhysicalSettings.FreeGridMm));
        var file=Environment.GetEnvironmentVariable("CIRCUIT_EDITOR_UNO_REVIEW");
        if(!string.IsNullOrEmpty(file))
        {
            const double Scale=9;var bounds=drawing.Bounds;var visual=new DrawingVisual();using(var dc=visual.RenderOpen())
            {dc.DrawRectangle(Brushes.WhiteSmoke,null,new Rect(0,0,680,540));dc.PushTransform(new TranslateTransform(25,25));dc.PushTransform(new ScaleTransform(Scale,Scale));dc.DrawDrawing(drawing);}
            var bitmap=new RenderTargetBitmap(680,540,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(file);encoder.Save(stream);
        }
        foreach(var definition in project.Library)
        {
            using var p=TestProjectFactory.Create();using var s=TestProjectFactory.Mockup(p);var item=p.Add(definition)!;item.MockupPlacement.MoveTo(5,5);
            for(var rotation=0;rotation<4;rotation++)
            {
                foreach(var c in item.Footprint.Contacts)
                {
                    var point=ContactGeometry.World(item.Footprint,c,item.MockupPlacement,PhysicalSettings.FreeGridMm);
                    Assert.Equal($"{item.Model.Id}:{c.Id}",PhysicalContactHover.Find(s,point)!.Value.Key);
                }
                item.MockupPlacement.Rotate();
            }
        }
    }
}
