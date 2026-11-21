using CircuitEditor.App.Controls.ThreeD;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.Tests;

public sealed class SceneInteractionTests
{
    [Fact]
    public void HiddenWorkspaceDoesNotRequestBuildsOrCaptureTheProject()
    {
        using var environment = new Stage4TestEnvironment();
        var workspace = new App.ViewModels.ThreeDViewModel(environment.Project);
        var invalidations = 0;
        workspace.SceneInvalidated += (_, _) => invalidations++;
        Assert.Null(workspace.Capture());
        workspace.SetActive(true);
        Assert.Equal(1, invalidations);
        Assert.NotNull(workspace.Capture());
        workspace.SetActive(false);
        environment.Project.ReplaceProject(BreadboardTestData.Create());
        Assert.Equal(1, invalidations);
        Assert.Null(workspace.Capture());
    }

    [Fact]
    public void DragThatReturnsToMouseDownPointDoesNotSelect()
    {
        var click = new SceneClick();
        click.Begin(10, 5);
        click.Move(18, 5);
        Assert.False(click.Release(10, 5));
        click.Begin(10, 5);
        Assert.True(click.Release(12, 4));
    }

[Fact]
    public void PickingUsesVisualIdentityAndCanForgetDetachedObjects()
    {
        ScenePickIndex<object> index = new();
        var visual = new object();
        var id = Guid.NewGuid();
        index.Set(visual, id);
        Assert.Equal(id, index.Find(visual));
        Assert.Null(index.Find(new object()));
        index.Remove(visual);
        Assert.Null(index.Find(visual));
    }

    [Fact]
    public void LayerFilterLeavesOrdinaryComponentsVisible()
    {
        var visibility = new SceneLayerVisibility(false, true, false, true);
        Assert.False(visibility.Shows(SceneLayer.Wire));
        Assert.False(visibility.Shows(SceneLayer.Board));
        Assert.True(visibility.Shows(SceneLayer.Breadboard));
        Assert.True(visibility.Shows(SceneLayer.Label));
        Assert.True(visibility.Shows(SceneLayer.Component));
    }
}
