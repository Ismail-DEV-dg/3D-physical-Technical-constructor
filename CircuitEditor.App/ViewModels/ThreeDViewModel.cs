using CircuitEditor.Core.History;
using CircuitEditor.Core.Scene;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;

namespace CircuitEditor.App.ViewModels;

public sealed partial class ThreeDViewModel(ProjectViewModel? project = null, ISceneBuilder? builder = null)
    : WorkspaceViewModel(WorkspaceKind.ThreeD, "3D", "04", "Трёхмерный вид", "Просмотр макета под любым углом")
{
    public event EventHandler? SceneInvalidated;
    public event EventHandler<SceneCameraView>? CameraRequested;
    public event EventHandler? SelectionChanged;

    [ObservableProperty] private bool showWires = true;
    [ObservableProperty] private bool showLabels = true;
    [ObservableProperty] private bool showBoards = true;
    [ObservableProperty] private bool showBreadboard = true;
    [ObservableProperty] private string notice = "";
    private bool active;
    private Guid? boardId;
    public Guid? SelectedId => project?.Wiring.Selected?.Id ?? project?.SelectedComponent?.Model.Id ?? boardId;
    public SceneLayerVisibility Layers => new(ShowWires, ShowLabels, ShowBoards, ShowBreadboard);

    public void SetActive(bool value)
    {
        if (active == value) return;
        active = value;
        if (project is null) return;
        if (active)
        {
            project.ModelCommitted += Invalidate;
            project.ProjectReplaced += Replace;
            project.PropertyChanged += ProjectChanged;
            SceneInvalidated?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            project.ModelCommitted -= Invalidate;
            project.ProjectReplaced -= Replace;
            project.PropertyChanged -= ProjectChanged;
        }
    }

    public void Pick(Guid? id, SceneLayer? layer)
    {
        boardId = layer == SceneLayer.Breadboard ? id : null;
        project?.Wiring.SelectForInspection(layer == SceneLayer.Wire ? id : null);
        project?.Select(layer is SceneLayer.Component or SceneLayer.Board
            ? project.Components.FirstOrDefault(c => c.Model.Id == id) : null);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public ProjectSnapshot? Capture() => active && project is not null ? new(project.Model) : null;
    public SceneModel Build(ProjectSnapshot snapshot, CancellationToken token) =>
        builder?.Build(snapshot.Restore(), token) ?? new([], default);

    [RelayCommand] private void Top() => Request(SceneCameraView.Top);
    [RelayCommand] private void Front() => Request(SceneCameraView.Front);
    [RelayCommand] private void Isometric() => Request(SceneCameraView.Isometric);
    [RelayCommand] private void Fit() => Request(SceneCameraView.Fit);
    [RelayCommand] private void ResetCamera() => Request(SceneCameraView.Initial);
    private void Request(SceneCameraView view) => CameraRequested?.Invoke(this, view);
    private void Invalidate(object? sender, EventArgs args) => SceneInvalidated?.Invoke(this, EventArgs.Empty);
    private void Replace(object? sender, EventArgs args) { boardId = null; Invalidate(sender, args); }
    private void ProjectChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(ProjectViewModel.SelectedComponent) or nameof(ProjectViewModel.Wiring))
            SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
