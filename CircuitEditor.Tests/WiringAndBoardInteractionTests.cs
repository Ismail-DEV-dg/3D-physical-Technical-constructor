using CircuitEditor.App.Interaction;
using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.History;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Wiring;
namespace CircuitEditor.Tests;
public sealed class WiringAndBoardInteractionTests
{
    [Fact]
    public void SwitchingOffAndEscClearPreviewAndAllowDragging()
    {
        using var e=new Stage4TestEnvironment();var editor=e.Project.Wiring;var b=e.Mockup.Board!;
        var point=BreadboardGeometry.World(b,b.Definition.Holes[0].Position);
        editor.IsWiring=true;editor.Handle(new(CanvasAction.Down),point);Assert.NotNull(editor.PreviewStart);
        editor.IsWiring=false;Assert.Null(editor.PreviewStart);Assert.False(editor.HasSelection);
        editor.IsWiring=true;e.Mockup.InputCommand.Execute(new(CanvasAction.Cancel));Assert.False(editor.IsWiring);
        Assert.Empty(e.Project.Model.Circuit.Wires);Assert.Equal(0,e.Project.History.Count);
        editor.IsWiring=true;editor.Handle(new(CanvasAction.Down),point);
        editor.Handle(new(CanvasAction.Down),BreadboardGeometry.World(b,b.Definition.Holes[1].Position));
        Assert.NotNull(editor.Selected);editor.IsWiring=false;Assert.False(editor.HasSelection);Assert.Single(e.Project.Model.Circuit.Wires);
    }
    [Fact]
    public void BoardTransformKeepsBindingsWiresHistoryAndFileRoundTrip()
    {
        using var e=new Stage4TestEnvironment();var p=e.Project;var b=e.Mockup.Board!;
        var c=p.Add(p.Library.Single(d=>d.Id=="resistor"))!;var h=b.Definition.Holes.Single(h=>h.Id=="left/A/01");
        Assert.True(e.Mockup.TryPlace(c,new(b.X+h.Position.X,b.Y+h.Position.Y,90)));
        var endpoint=WireEndpoint.Hole(b.Id,h.Id);var bindings=p.Model.Bindings.ToArray();var before=new ProjectSnapshot(p.Model);
        p.History.Clear();p.BeginGesture();p.Model.TransformBoard(b.Id,b.X+20,b.Y+30,90);p.EndGesture();p.Refresh();
        Assert.Equal(bindings,p.Model.Bindings);var expected=BreadboardGeometry.World(e.Mockup.Board!,h.Position);
        Assert.Equal(expected,WireGeometry.Resolve(p.Model,endpoint));
        var contact=ContactGeometry.World(c.Footprint,c.Footprint.Contacts[0],c.MockupPlacement,b.Definition.HolePitch);
        Assert.Equal(expected.X,contact.X,7);Assert.Equal(expected.Y,contact.Y,7);
        var file=System.IO.Path.Combine(e.Folder,"rotated.circ");Assert.Null(e.Repository.Save(file,e.Session.Capture()));Assert.True(e.Session.Load(file),p.StatusMessage);
        Assert.Equal(90,e.Mockup.Board!.Rotation);
        p.ReplaceProject(before.Restore());p.BeginGesture();p.Model.TransformBoard(b.Id,b.X,b.Y,90);p.EndGesture();
        p.UndoCommand.Execute(null);Assert.True(before.Matches(p.Model));p.RedoCommand.Execute(null);Assert.Equal(90,e.Mockup.Board!.Rotation);
    }
    [Fact]
    public void BoardDragIsOneGestureAndEscRestoresIt()
    {
        using var e=new Stage4TestEnvironment();var vm=e.Mockup;var b=vm.Board!;var before=new ProjectSnapshot(e.Project.Model);
        var edge=new LayoutPoint(b.X+1,b.Y+1);
        Assert.True(vm.BoardInteraction.Handle(new(CanvasAction.Down),edge));
        vm.BoardInteraction.Handle(new(CanvasAction.Move),new(edge.X+20,edge.Y+10));
        vm.BoardInteraction.Handle(new(CanvasAction.Cancel),edge);Assert.True(before.Matches(e.Project.Model));Assert.Equal(0,e.Project.History.Count);
        vm.BoardInteraction.Handle(new(CanvasAction.Down),edge);vm.BoardInteraction.Handle(new(CanvasAction.Move),new(edge.X+20,edge.Y+10));vm.BoardInteraction.Handle(new(CanvasAction.Up),edge);
        Assert.Equal(1,e.Project.History.Count);e.Project.UndoCommand.Execute(null);Assert.True(before.Matches(e.Project.Model));
    }
}
