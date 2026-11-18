using System.IO;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.History;
namespace CircuitEditor.Tests;
public sealed class SessionLifecycleTests
{
    [Fact]
    public void AutosaveSkipsActiveGestureAndRestoresWithoutMarkingClean()
    {
        using var e=new Stage4TestEnvironment(); e.Session.Initialize();
        e.Project.Add(e.Project.Library[0]); e.Project.BeginGesture(); e.Session.Autosave();
        Assert.False(e.Repository.HasRecovery); e.Project.EndGesture(); e.Tick!(); Assert.True(e.Repository.HasRecovery);
        var before=new ProjectSnapshot(e.Project.Model);
        e.Project.ReplaceProject(new Core.Models.CircuitProject(footprints:e.Project.Model.Footprints,breadboards:e.Project.Model.Breadboards));
        e.Recover=true; e.Session.Initialize();
        Assert.True(before.Matches(e.Project.Model)); Assert.True(e.Project.IsDirty); Assert.False(e.Project.CanUndo);
        e.Decision=SaveDecision.Discard; Assert.True(e.Session.RequestClose()); Assert.False(e.Repository.HasRecovery);
    }
    [Fact]
    public void OpeningRestoresBothCamerasAndClearsHistory()
    {
        using var e=new Stage4TestEnvironment(); e.Project.Add(e.Project.Library[0]);
        e.Mockup.Zoom=2; e.Mockup.OffsetX=-19; e.Schematic.Zoom=.5; e.Schematic.OffsetY=123;
        e.Navigation.Navigate(WorkspaceKind.Schematic);
        var path=Path.Combine(e.Folder,"camera.circ"); Assert.Null(e.Repository.Save(path,e.Session.Capture()));
        e.Mockup.Zoom=1; e.Schematic.OffsetY=0;
        Assert.True(e.Session.Load(path)); Assert.Equal(2,e.Mockup.Zoom); Assert.Equal(-19,e.Mockup.OffsetX); Assert.Equal(123,e.Schematic.OffsetY);
        Assert.Equal(WorkspaceKind.Schematic,e.Navigation.Current.Kind); Assert.False(e.Project.IsDirty); Assert.Equal(0,e.Project.History.Count);
    }
    [Fact]
    public void CancelNewOrOpenAndFailedSavePreserveCurrentDocument()
    {
        using var e=new Stage4TestEnvironment(); e.Project.Add(e.Project.Library[0]); var before=new ProjectSnapshot(e.Project.Model);
        e.Session.NewCommand.Execute(null); Assert.True(before.Matches(e.Project.Model));
        e.Session.OpenCommand.Execute(null); Assert.True(before.Matches(e.Project.Model));
        e.Decision=SaveDecision.Save; e.Target=e.Folder; // directory is not a writable project file
        Assert.False(e.Session.RequestClose()); Assert.True(e.Project.IsDirty); Assert.True(before.Matches(e.Project.Model));
        e.Decision=SaveDecision.Discard; e.Session.NewCommand.Execute(null);
        Assert.Empty(e.Project.Components); Assert.False(e.Project.IsDirty); Assert.False(e.Project.CanUndo);
    }
}
