using CircuitEditor.Core.History;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Wiring;
using CircuitEditor.Core.Placement;
using CircuitEditor.App.Interaction;
namespace CircuitEditor.Tests;
public sealed class Stage4HistoryTests
{
    [Theory]
    [InlineData("add")][InlineData("move")][InlineData("rotate")][InlineData("delete")][InlineData("value")]
    [InlineData("wire-add")][InlineData("wire-color")][InlineData("wire-nodes")][InlineData("wire-delete")]
    public void EveryEditRestoresExactlyOnUndoAndRedo(string action)
    {
        using var e=new Stage4TestEnvironment(); var p=e.Project;
        var c=p.Add(p.Library.Single(d=>d.Id=="resistor"))!;
        var board=e.Mockup.Board!;
        var wire=new Wire(WireEndpoint.Contact(c.Model.Id,c.Footprint.Contacts[0].Id),WireEndpoint.Hole(board.Id,board.Definition.Holes[0].Id));
        if(action.StartsWith("wire-") && action!="wire-add") { p.Model.Circuit.PutWire(wire); p.Refresh(); }
        p.History.Clear(); var before=new ProjectSnapshot(p.Model); p.BeginGesture(action);
        switch(action)
        {
            case "add": p.Add(p.Library.Single(d=>d.Id=="led")); break;
            case "move": c.MockupPlacement.MoveTo(10,20); break;
            case "rotate": c.SchematicPlacement.Rotate(); break;
            case "delete": p.RemoveSelected(); break;
            case "value": c.ResistorEditor!.SelectedOption=c.ResistorEditor.Options.First(o=>o.Ohms==470); break;
            case "wire-add": p.Model.Circuit.PutWire(wire); break;
            case "wire-color": p.Model.Circuit.PutWire(new Wire(wire.Id,wire.Start,wire.End,WirePalette.Colors[2])); break;
            case "wire-nodes": p.Model.Circuit.PutWire(new Wire(wire.Id,wire.Start,wire.End,wire.Color,[new LayoutPoint(10,50)])); break;
            case "wire-delete": p.Model.Circuit.RemoveWire(wire.Id); break;
        }
        p.EndGesture(); Assert.Equal(1,p.History.Count); var after=new ProjectSnapshot(p.Model);
        p.UndoCommand.Execute(null); Assert.True(before.Matches(p.Model));
        p.RedoCommand.Execute(null); Assert.True(after.Matches(p.Model));
    }
    [Fact]
    public void OneGestureIncludesManySliderChangesAndEscRecordsNothing()
    {
        using var e=new Stage4TestEnvironment(); var p=e.Project;
        var c=p.Add(p.Library.Single(d=>d.Id=="dht11"))!; p.History.Clear();
        p.BeginGesture("slider");
        foreach(var v in Enumerable.Range(26,20)) c.InputsEditor!.Parameters[0].Value=v;
        p.EndGesture(); Assert.Equal(1,p.History.Count);
        var before=new ProjectSnapshot(p.Model); p.BeginGesture(); c.MockupPlacement.MoveTo(5,80); p.Refresh(); p.CancelGesture();
        Assert.Equal(1,p.History.Count); Assert.True(before.Matches(p.Model));
        p.UndoCommand.Execute(null); Assert.Equal(25,p.Components[0].Model.Inputs.Values[Core.Inputs.ComponentInput.Temperature]);
    }
    [Fact]
    public void HistoryIsBoundedAndBranchingInvalidatesSavedCheckpoint()
    {
        using var e=new Stage4TestEnvironment(); var p=e.Project;
        var c=p.Add(p.Library[0])!; p.History.Clear();
        for(var i=1;i<=220;i++) { c.SchematicPlacement.MoveTo(i,0); p.Refresh(); }
        Assert.Equal(200,p.History.Count); p.History.MarkSaved(); Assert.False(p.IsDirty);
        p.UndoCommand.Execute(null); Assert.True(p.IsDirty); Assert.True(p.CanRedo);
        p.Components[0].SchematicPlacement.MoveTo(999,0); p.Refresh(); Assert.False(p.CanRedo); Assert.True(p.IsDirty);
    }
    [Fact]
    public void CopyPastesOnlyInternalWiresWithFreshIdsAndNoBoardBindings()
    {
        using var e=new Stage4TestEnvironment(); var p=e.Project;
        var a=p.Add(p.Library.Single(d=>d.Id=="resistor"))!; var b=p.Add(p.Library.Single(d=>d.Id=="led"))!;
        var endA=WireEndpoint.Contact(a.Model.Id,a.Footprint.Contacts[0].Id); var endB=WireEndpoint.Contact(b.Model.Id,b.Footprint.Contacts[0].Id);
        p.Model.Circuit.PutWire(new Wire(endA,endB)); p.Model.Circuit.PutWire(new Wire(endA,WireEndpoint.Hole(e.Mockup.Board!.Id,e.Mockup.Board.Definition.Holes[0].Id)));
        a.IsSelected=b.IsSelected=true; p.Refresh(); p.History.Clear();
        p.CopyCommand.Execute(null); p.PasteCommand.Execute(null);
        Assert.Equal(4,p.ComponentCount); Assert.Equal(3,p.Model.Circuit.Wires.Count); Assert.Equal(1,p.History.Count);
        Assert.Equal(4,p.Components.Select(c=>c.Reference).Distinct().Count()); Assert.Empty(p.Model.Bindings);
        p.UndoCommand.Execute(null); Assert.Equal(2,p.ComponentCount); Assert.Equal(2,p.Model.Circuit.Wires.Count);
    }
}
