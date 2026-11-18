namespace CircuitEditor.Tests;
public sealed class ConnectivityViewModelTests
{
    [Fact]
    public void RebuildHappensAtCommitUndoRedoAndOpenButNotEachGestureFrame()
    {
        using var p=TestProjectFactory.Create();var c=p.Add(p.Library[0])!;var count=p.Connectivity.BuildCount;
        p.BeginGesture();for(var i=1;i<=20;i++){ c.SchematicPlacement.MoveTo(i,0);p.Refresh(); }
        Assert.Equal(count,p.Connectivity.BuildCount);p.EndGesture();Assert.Equal(count+1,p.Connectivity.BuildCount);
        p.UndoCommand.Execute(null);Assert.Equal(count+2,p.Connectivity.BuildCount);
        p.RedoCommand.Execute(null);Assert.Equal(count+3,p.Connectivity.BuildCount);
        p.Select(null);p.Refresh();Assert.Equal(count+3,p.Connectivity.BuildCount);
    }
}
