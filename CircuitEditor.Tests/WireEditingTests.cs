using CircuitEditor.App.Interaction;
using CircuitEditor.Core.History;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Wiring;
namespace CircuitEditor.Tests;
public sealed class WireEditingTests
{
    [Fact]
    public void HoveringShortWireShowsBothBreadboardHolesInOneLabel()
    {
        using var environment = new Stage4TestEnvironment();
        var project = environment.Project;
        var board = environment.Mockup.Board!;
        var start = WireEndpoint.Hole(board.Id, "left/C/08");
        var end = WireEndpoint.Hole(board.Id, "left/D/08");
        var first = WireGeometry.Resolve(project.Model, start)!.Value;
        var last = WireGeometry.Resolve(project.Model, end)!.Value;
        var wire = new Wire(start, end);
        project.Model.Circuit.PutWire(wire);
        project.Refresh();

        project.Wiring.Handle(new(CanvasAction.Move), new((first.X + last.X) / 2, (first.Y + last.Y) / 2));
        Assert.Equal("Провод на макетной плате: C8 — D8", project.Wiring.EndpointSummary);
        project.Wiring.Handle(new(CanvasAction.Move), new(-10, -10));
        Assert.Null(project.Wiring.EndpointSummary);
    }

    [Fact]
    public void EndpointFollowsMovingComponentAndDuplicateIsDirectionIndependent()
    {
        using var e=new Stage4TestEnvironment(); var p=e.Project; var c=p.Add(p.Library.Single(d=>d.Id=="resistor"))!;
        var a=WireEndpoint.Contact(c.Model.Id,c.Footprint.Contacts[0].Id);
        var b=WireEndpoint.Hole(e.Mockup.Board!.Id,e.Mockup.Board.Definition.Holes[0].Id);
        var wire=new Wire(a,b); Assert.Null(WireGeometry.Validate(p.Model,wire)); p.Model.Circuit.PutWire(wire);
        Assert.Contains("существует",WireGeometry.Validate(p.Model,new Wire(b,a))!);
        var before=WireGeometry.Resolve(p.Model,a)!.Value;
        c.MockupPlacement.MoveTo(c.MockupPlacement.X+5.08,c.MockupPlacement.Y);
        Assert.Equal(before.X+5.08,WireGeometry.Resolve(p.Model,a)!.Value.X,8); Assert.Equal(a,wire.Start);
        Assert.NotNull(WireGeometry.Validate(p.Model,new Wire(WireEndpoint.Contact(Guid.NewGuid(),"absent"),b)));
    }
    [Fact]
    public void ZeroPhysicalLengthIsRejectedEvenWithDifferentEndpointKinds()
    {
        using var e=new Stage4TestEnvironment(); var p=e.Project; var c=p.Add(p.Library.Single(d=>d.Id=="resistor"))!;
        var board=e.Mockup.Board!; var hole=board.Definition.Holes[0];
        c.MockupPlacement.MoveTo(board.X+hole.Position.X,board.Y+hole.Position.Y);
        var wire=new Wire(WireEndpoint.Contact(c.Model.Id,c.Footprint.Contacts[0].Id),WireEndpoint.Hole(board.Id,hole.Id));
        Assert.Contains("Нулевая",WireGeometry.Validate(p.Model,wire)!);
    }
    [Fact]
    public void CreationPreviewEscAndInvalidEndDragHaveNoPartialHistory()
    {
        using var e=new Stage4TestEnvironment(); var p=e.Project; var editor=p.Wiring;
        var board=e.Mockup.Board!; var holes=board.Definition.Holes.Take(2).ToArray();
        LayoutPoint Point(int i)=>new(board.X+holes[i].Position.X,board.Y+holes[i].Position.Y);
        editor.IsWiring=true;
        editor.Handle(new(CanvasAction.Down),Point(0)); Assert.NotNull(editor.PreviewStart);
        editor.Handle(new(CanvasAction.Up),Point(0)); Assert.NotNull(editor.PreviewStart);
        editor.Handle(new(CanvasAction.Cancel),Point(0)); Assert.Null(editor.PreviewStart); Assert.False(p.CanUndo);
        editor.Handle(new(CanvasAction.Down),Point(0)); editor.Handle(new(CanvasAction.Down),Point(1));
        Assert.Single(p.Model.Circuit.Wires); Assert.Equal(1,p.History.Count);
        var snapshot=new ProjectSnapshot(p.Model);
        editor.Handle(new(CanvasAction.Down),Point(0)); editor.Handle(new(CanvasAction.Move),new(-100,-100)); editor.Handle(new(CanvasAction.Up),new(-100,-100));
        Assert.True(snapshot.Matches(p.Model)); Assert.Equal(1,p.History.Count);
    }
    [Fact]
    public void InsertionAndOccupiedHoleStateUndoTogether()
    {
        using var e=new Stage4TestEnvironment(); var p=e.Project; var c=p.Add(p.Library.Single(d=>d.Id=="resistor"))!;
        var board=e.Mockup.Board!; var hole=board.Definition.Holes.Single(h=>h.Id=="left/A/01");
        p.History.Clear(); p.BeginGesture("insert");
        Assert.True(e.Mockup.TryPlace(c,new(board.X+hole.Position.X,board.Y+hole.Position.Y,90)));
        p.EndGesture(); Assert.Equal(2,p.Model.Bindings.Count); var bound=new ProjectSnapshot(p.Model);
        p.UndoCommand.Execute(null); Assert.Empty(p.Model.Bindings);
        p.RedoCommand.Execute(null); Assert.True(bound.Matches(p.Model));
        var doc=e.Session.Capture();
        var restored=Core.Documents.ProjectDocumentResolver.Resolve(doc,p.Library,p.Model.Footprints,board.Definition,new Core.Services.ComponentInsertionService());
        Assert.True(bound.Matches(restored));
    }
    [Fact]
    public void ReattachingEndpointAndRecoloringAreIndependentUndoSteps()
    {
        using var e=new Stage4TestEnvironment(); var p=e.Project; var editor=p.Wiring; var b=e.Mockup.Board!;
        var holes=b.Definition.Holes.Take(3).ToArray();
        LayoutPoint Point(int i)=>new(b.X+holes[i].Position.X,b.Y+holes[i].Position.Y);
        editor.IsWiring=true; editor.Handle(new(CanvasAction.Down),Point(0)); editor.Handle(new(CanvasAction.Down),Point(1));
        p.History.Clear();
        editor.Handle(new(CanvasAction.Down),Point(1)); editor.Handle(new(CanvasAction.Move),Point(2)); editor.Handle(new(CanvasAction.Up),Point(2));
        Assert.Equal(holes[2].Id,p.Model.Circuit.Wires[0].End.TerminalId); Assert.Equal(1,p.History.Count);
        editor.Color=WirePalette.Colors[3]; Assert.Equal(2,p.History.Count);
        p.UndoCommand.Execute(null); Assert.Equal(WirePalette.Colors[0],p.Model.Circuit.Wires[0].Color);
        p.UndoCommand.Execute(null); Assert.Equal(holes[1].Id,p.Model.Circuit.Wires[0].End.TerminalId);
    }
    [Fact]
    public void MovingInsertedComponentKeepsContactEndpointWhileChangingHole()
    {
        using var e=new Stage4TestEnvironment(); var p=e.Project; var c=p.Add(p.Library.Single(d=>d.Id=="resistor"))!;
        var b=e.Mockup.Board!; var hole=b.Definition.Holes.Single(h=>h.Id=="left/A/01");
        Assert.True(e.Mockup.TryPlace(c,new(b.X+hole.Position.X,b.Y+hole.Position.Y,90)));
        var endpoint=WireEndpoint.Contact(c.Model.Id,c.Footprint.Contacts[0].Id);
        var wire=new Wire(endpoint,WireEndpoint.Hole(b.Id,b.Definition.Holes.Last().Id)); p.Model.Circuit.PutWire(wire); p.Refresh();
        var oldBinding=p.Model.Bindings.First(x=>x.ContactId==endpoint.TerminalId);
        var initial=WireGeometry.Resolve(p.Model,endpoint)!.Value;
        Assert.True(e.Mockup.TryPlace(c,new(c.MockupPlacement.X,c.MockupPlacement.Y+b.Definition.HolePitch,90)));
        Assert.NotEqual(oldBinding.HoleId,p.Model.Bindings.First(x=>x.ContactId==endpoint.TerminalId).HoleId);
        Assert.Equal(endpoint,p.Model.Circuit.Wires.Single().Start);
        Assert.Equal(initial.Y+b.Definition.HolePitch,WireGeometry.Resolve(p.Model,endpoint)!.Value.Y,8);
    }

}
