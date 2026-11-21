using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Scene;
using HelixToolkit.Wpf;

namespace CircuitEditor.App.Controls.ThreeD;

public sealed partial class SceneViewport : HelixViewport3D
{
    private readonly ScenePickIndex<Visual3D> picks = new();
    private readonly Dictionary<Guid, SceneVisualNode> nodes = [];
    private readonly ModelVisual3D sceneRoot = new();
    private readonly SceneClick click = new();
    private ThreeDViewModel? workspace;
    private ScenePreparation? preparation;
    private CancellationTokenSource? pending;
    private SceneBounds bounds;
    private bool initialized;
    private Guid? hovered;

    public SceneViewport()
    {
        RotateGesture = new MouseGesture(MouseAction.RightClick);
        PanGesture = new MouseGesture(MouseAction.MiddleClick);
        RotateGesture2 = new MouseGesture(MouseAction.None);
        PanGesture2 = new MouseGesture(MouseAction.None);
        IsHeadLightEnabled = false;
        ShowCoordinateSystem = false;
        ShowViewCube = false;
        ModelUpDirection = new(0, 0, 1);
        ToolTip = new System.Windows.Controls.ToolTip { Visibility = Visibility.Collapsed };
        Children.Add(sceneRoot);
        Loaded += (_, _) => Connect();
        Unloaded += (_, _) => Disconnect();
        IsVisibleChanged += (_, _) => { if (IsVisible) Connect(); else Disconnect(); };
        DataContextChanged += (_, _) => { Disconnect(); Connect(); };
        ContextMenuOpening += (_, args) => args.Handled = true;
    }

    private void Connect()
    {
        if (!IsLoaded || !IsVisible || workspace is not null || DataContext is not ThreeDViewModel vm) return;
        if (TryFindResource(typeof(ISceneMeshFactory)) is not ISceneMeshFactory factory) return;
        preparation ??= new(factory);
        workspace = vm;
        vm.SceneInvalidated += Rebuild;
        vm.CameraRequested += MoveCamera;
        vm.SelectionChanged += SelectionChanged;
        vm.PropertyChanged += LayersChanged;
        vm.SetActive(true);
    }

    private void Disconnect()
    {
        var vm = workspace;
        if (vm is null) return;
        pending?.Cancel();
        pending = null;
        vm.SetActive(false);
        vm.SceneInvalidated -= Rebuild;
        vm.CameraRequested -= MoveCamera;
        vm.SelectionChanged -= SelectionChanged;
        vm.PropertyChanged -= LayersChanged;
        workspace = null;
        click.Cancel();
        SetHover(null);
    }

    private async void Rebuild(object? sender, EventArgs args)
    {
        if (workspace is not { } vm || vm.Capture() is not { } snapshot || preparation is not { } renderer) return;
        pending?.Cancel();
        using CancellationTokenSource request = new();
        pending = request;
        try
        {
            var prepared = await Task.Run(() => renderer.Build(vm.Build(snapshot, request.Token), request.Token), request.Token);
            if (pending != request) return;
            Apply(prepared);
            vm.Notice = "";
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested) { }
        catch (Exception failure) when (failure is not OutOfMemoryException)
        {
            if (pending == request) vm.Notice = $"Не удалось построить 3D-вид. {failure.Message}";
        }
        finally { if (pending == request) pending = null; }
    }

    private void Apply(PreparedScene prepared)
    {
        foreach (var previous in nodes.Values) Unregister(previous);
        nodes.Clear();
        sceneRoot.Children.Clear();
        sceneRoot.Content = prepared.Lights;
        bounds = prepared.Bounds;
        foreach (var item in prepared.Nodes)
        {
            SceneVisualNode visual = new(item);
            nodes.Add(item.Node.Id, visual);
            foreach (var part in visual.Body.Children) picks.Set(part, item.Node.Id);
        }
        UpdateLayers();
        if (!initialized && nodes.Count > 0 && Camera is ProjectionCamera camera)
        {
            SceneCamera.Apply(camera, bounds, SceneCameraView.Initial, Aspect, false);
            initialized = true;
        }
    }

    private void Unregister(SceneVisualNode old)
    {
        foreach (var part in old.Body.Children) picks.Remove(part);
    }

    private void MoveCamera(object? sender, SceneCameraView view)
    {
        if (Camera is ProjectionCamera camera) SceneCamera.Apply(camera, bounds, view, Aspect, true);
    }

    private double Aspect => ActualWidth / Math.Max(1, ActualHeight);
    private void SelectionChanged(object? sender, EventArgs args) => UpdateLayers();
    private void LayersChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(ThreeDViewModel.ShowBoards) or nameof(ThreeDViewModel.ShowBreadboard)
            or nameof(ThreeDViewModel.ShowLabels) or nameof(ThreeDViewModel.ShowWires)) UpdateLayers();
    }

    private void UpdateLayers()
    {
        if (workspace is not { } vm) return;
        foreach (var visual in nodes.Values)
        {
            var show = vm.Layers.Shows(visual.Node.Layer);
            Attach(visual.Body, show);
            Attach(visual.Labels, show && vm.Layers.Labels && visual.Labels.Children.Count != 0);
            Attach(visual.Outline, show && vm.SelectedId == visual.Node.Id && visual.Node.Layer != SceneLayer.Wire);
            if (visual.Node.Layer == SceneLayer.Wire)
                ((ModelVisual3D)visual.Body.Children[0]).Content =
                    vm.SelectedId == visual.Node.Id ? visual.Source.Outline : visual.Source.Parts[0].Model;
        }
    }

    private void Attach(ModelVisual3D visual, bool visible)
    {
        var attached = visual.GetValue(AttachedProperty) is true;
        if (visible == attached) return;
        if (visible) sceneRoot.Children.Add(visual);
        else sceneRoot.Children.Remove(visual);
        visual.SetValue(AttachedProperty, visible);
    }

    private static readonly DependencyProperty AttachedProperty = DependencyProperty.RegisterAttached(
        "Attached", typeof(bool), typeof(SceneViewport), new PropertyMetadata(false));

    protected override void OnPreviewMouseDown(MouseButtonEventArgs args)
    {
        if (Viewport is { } viewport && args.ChangedButton == MouseButton.Left)
        {
            var at = args.GetPosition(viewport);
            click.Begin(at.X, at.Y);
        }
        else click.Cancel();
        base.OnPreviewMouseDown(args);
    }

    protected override void OnPreviewMouseUp(MouseButtonEventArgs args)
    {
        if (Viewport is { } viewport && args.ChangedButton == MouseButton.Left)
        {
            var at = args.GetPosition(viewport);
            if (click.Release(at.X, at.Y))
            {
                var node = Find(at);
                workspace?.Pick(node?.Id, node?.Layer);
            }
        }
        base.OnPreviewMouseUp(args);
    }

    protected override void OnPreviewMouseMove(MouseEventArgs args)
    {
        if (Viewport is { } viewport)
        {
            var at = args.GetPosition(viewport);
            click.Move(at.X, at.Y);
            if (args.LeftButton == MouseButtonState.Released && args.RightButton == MouseButtonState.Released &&
                args.MiddleButton == MouseButtonState.Released) SetHover(Find(at));
            else SetHover(null);
        }
        base.OnPreviewMouseMove(args);
    }

    protected override void OnMouseLeave(MouseEventArgs args)
    {
        click.Cancel();
        SetHover(null);
        base.OnMouseLeave(args);
    }

    private SceneNode? Find(Point at)
    {
        if (Viewport is null) return null;
        foreach (var hit in Viewport3DHelper.FindHits(Viewport, at))
            if (hit.Visual is { } visual && picks.Find(visual) is { } id &&
                nodes.TryGetValue(id, out var found)) return found.Node;
        return null;
    }

    private void SetHover(SceneNode? node)
    {
        if (hovered == node?.Id) return;
        hovered = node?.Id;
        var tooltip = (System.Windows.Controls.ToolTip)ToolTip;
        tooltip.IsOpen = false;
        tooltip.Visibility = node is null ? Visibility.Collapsed : Visibility.Visible;
        tooltip.Content = node is null ? null : string.Join(" · ",
            new[] { node.Label, node.Name, node.Value, node.Color }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
