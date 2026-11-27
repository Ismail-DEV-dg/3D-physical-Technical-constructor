using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Scene;
using HelixToolkit.Wpf;

namespace CircuitEditor.App.Controls.ThreeD;

public sealed class SceneViewport : HelixViewport3D
{
    private readonly ScenePickIndex<Visual3D> picks = new();
    private readonly Dictionary<Guid, SceneVisualNode> nodes = [];
    private readonly ModelVisual3D sceneRoot = new();
    private readonly ModelVisual3D shadowRoot = new();
    private readonly SceneClick click = new();
    private readonly SceneViewportSimulation playback;
    private readonly SceneHover hover;
    private ThreeDViewModel? workspace;
    private ScenePreparation? preparation;
    private CancellationTokenSource? pending;
    private PreparedScene? applied;
    private SceneBounds bounds;
    internal event EventHandler? SceneApplied;
    private const double MinimumLabelHeight = 8;
    internal bool HasScene => applied is not null && IsVisible;
    private ISceneViewportRegistration? exportTarget;
    private bool initialized;
    private bool labelsReadable = true;
    internal ThreeDViewModel? Workspace => workspace;
    internal IReadOnlyDictionary<Guid, SceneVisualNode> SceneNodes => nodes;

    public SceneViewport()
    {
        playback = new(this);
        hover = new(this);
        RotateGesture = new MouseGesture(MouseAction.RightClick);
        PanGesture = new MouseGesture(MouseAction.MiddleClick);
        RotateGesture2 = new MouseGesture(MouseAction.None);
        PanGesture2 = new MouseGesture(MouseAction.None);
        ZoomGesture = new MouseGesture(MouseAction.None);
        ZoomGesture2 = new MouseGesture(MouseAction.None);
        IsHeadLightEnabled = false;
        ShowCoordinateSystem = false;
        ShowViewCube = false;
        ModelUpDirection = new(0, 0, 1);
        Children.Add(shadowRoot);
        Children.Add(sceneRoot);
        Loaded += (_, _) => Connect();
        Unloaded += (_, _) => Disconnect();
        IsVisibleChanged += (_, _) => { if (IsVisible) Connect(); else Disconnect(); };
        DataContextChanged += (_, _) => { Disconnect(); Connect(); };
        CameraChanged += (_, _) => EnsureLabelScale();
        SizeChanged += (_, _) => EnsureLabelScale();
        ContextMenuOpening += (_, args) => args.Handled = true;
    }

    private void Connect()
    {
        if (!IsLoaded || !IsVisible || workspace is not null || DataContext is not ThreeDViewModel vm) return;
        if (TryFindResource(typeof(ISceneMeshFactory)) is not ISceneMeshFactory factory) return;
        preparation ??= new(factory);
        workspace = vm;
        exportTarget = TryFindResource(typeof(ISceneViewportRegistration)) as ISceneViewportRegistration;
        exportTarget?.Attach(this);
        vm.SceneInvalidated += Rebuild;
        vm.CameraRequested += MoveCamera;
        vm.SelectionChanged += SelectionChanged;
        vm.PropertyChanged += LayersChanged;
        if (vm.Simulation is { } sim)
        {
            sim.FrameChanged += playback.QueueFrame;
            sim.PropertyChanged += playback.SimulationChanged;
        }
        vm.SetActive(true);
    }

    private void Disconnect()
    {
        var vm = workspace;
        if (vm is null) return;
        playback.ReleaseInteraction();
        playback.ClearFrames();
        if (vm.Simulation is { } sim)
        {
            sim.FrameChanged -= playback.QueueFrame;
            sim.PropertyChanged -= playback.SimulationChanged;
        }
        pending?.Cancel();
        pending = null;
        vm.SetActive(false);
        vm.SceneInvalidated -= Rebuild;
        vm.CameraRequested -= MoveCamera;
        vm.SelectionChanged -= SelectionChanged;
        vm.PropertyChanged -= LayersChanged;
        exportTarget?.Detach(this);
        exportTarget = null;
        workspace = null;
        click.Cancel();
        SetHover(null);
    }

    private void Rebuild(object? sender, EventArgs args)
    {
        if (workspace?.Capture() is { } snapshot) Prepare(snapshot);
    }

    internal async void Prepare(CircuitEditor.Core.History.ProjectSnapshot snapshot)
    {
        if (workspace is not { } vm || preparation is not { } renderer) return;
        pending?.Cancel();
        using CancellationTokenSource request = new();
        pending = request;
        try
        {
            var previous = applied;
            var prepared = await Task.Run(() => renderer.Build(vm.Build(snapshot, request.Token), request.Token, previous), request.Token);
            if (pending != request) return;
            Apply(prepared);
            vm.Notice = "";
            SceneApplied?.Invoke(this, EventArgs.Empty);
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
        playback.ClearFrames();
        applied = prepared;
        var remaining = prepared.Nodes.Select(item => item.Node.Id).ToHashSet();
        foreach (var old in nodes.Values.Where(item => !remaining.Contains(item.Node.Id)).ToArray())
            Remove(old);
        if (!ReferenceEquals(sceneRoot.Content, prepared.Lights)) sceneRoot.Content = prepared.Lights;
        bounds = prepared.Bounds;
        foreach (var item in prepared.Nodes)
        {
            if (nodes.TryGetValue(item.Node.Id, out var previous))
            {
                if (ReferenceEquals(previous.Source, item)) continue;
                Remove(previous);
            }
            SceneVisualNode visual = new(item);
            nodes.Add(item.Node.Id, visual);
            foreach (var part in visual.Body.Children) picks.Set(part, item.Node.Id);
        }
        UpdateLayers();
        playback.QueueFrame(this, workspace?.Simulation?.State ?? new());
        if (!initialized && nodes.Count > 0 && Camera is ProjectionCamera camera)
        {
            SceneCamera.Apply(camera, bounds, SceneCameraView.Initial, Aspect, false);
            initialized = true;
        }
    }

    private void Remove(SceneVisualNode old)
    {
        nodes.Remove(old.Node.Id);
        Attach(old.Body, false);
        Attach(old.Labels, false);
        Attach(old.Outline, false);
        if (old.Shadow is { } shadow) Attach(shadow, false, shadowRoot);
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
        var selection = vm.SelectedId;
        foreach (var visual in nodes.Values)
        {
            var show = vm.Layers.Shows(visual.Node.Layer);
            if (visual.Shadow is { } shadow)
                Attach(shadow, show, shadowRoot);
            Attach(visual.Body, show);
            Attach(visual.Labels, show && labelsReadable && vm.Layers.Labels && visual.Labels.Children.Count != 0);
            Attach(visual.Outline, show && selection == visual.Node.Id && visual.Node.Layer != SceneLayer.Wire);
            if (visual.Node.Layer == SceneLayer.Wire)
                visual.SelectWire(selection == visual.Node.Id);
        }
    }

    private void EnsureLabelScale()
    {
        if (workspace is null || Camera is not PerspectiveCamera camera) return;
        const double labelHeightMm = 3;
        var visibleHeight = 2 * camera.LookDirection.Length * Math.Tan(camera.FieldOfView * Math.PI / 360);
        var readable = visibleHeight > 0 && labelHeightMm * ActualWidth / visibleHeight >= MinimumLabelHeight;
        if (readable == labelsReadable) return;
        labelsReadable = readable;
        UpdateLayers();
    }

    internal string? ExportWithoutShadows(Func<string?> export)
    {
        Children.Remove(shadowRoot);
        try { return export(); }
        finally { Children.Insert(0, shadowRoot); }
    }

    private void Attach(ModelVisual3D visual, bool visible, ModelVisual3D? root = null)
    {
        root ??= sceneRoot;
        var attached = ReferenceEquals(System.Windows.Media.VisualTreeHelper.GetParent(visual), root);
        if (visible == attached) return;
        if (visible) root.Children.Add(visual);
        else root.Children.Remove(visual);
    }

    protected override void OnPreviewMouseDown(MouseButtonEventArgs args)
    {
        if (Viewport is { } viewport && args.ChangedButton == MouseButton.Left)
        {
            var at = args.GetPosition(viewport);
            click.Begin(at.X, at.Y);
            if (playback.BeginInteraction(at)) args.Handled = true;
        }
        else click.Cancel();
        base.OnPreviewMouseDown(args);
    }

    protected override void OnPreviewMouseUp(MouseButtonEventArgs args)
    {
        if (Viewport is { } viewport && args.ChangedButton == MouseButton.Left)
        {
            var at = args.GetPosition(viewport);
            playback.ReleaseInteraction();
            if (click.Release(at.X, at.Y))
            {
                var node = Find(at);
                workspace?.Pick(node?.Id, node?.Layer);
                if (node?.Family == "switch" && workspace?.Simulation is { HasSession: true } sim) sim.Switch(node.Id);
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
            if (args.LeftButton == MouseButtonState.Pressed) playback.MoveKnob(at);
            if (args.LeftButton == MouseButtonState.Released && args.RightButton == MouseButtonState.Released &&
                args.MiddleButton == MouseButtonState.Released) SetHover(Find(at));
            else SetHover(null);
        }
        base.OnPreviewMouseMove(args);
    }

    protected override void OnMouseLeave(MouseEventArgs args)
    {
        if (!playback.IsInteracting) click.Cancel();
        SetHover(null);
        base.OnMouseLeave(args);
    }

    protected override void OnLostMouseCapture(MouseEventArgs args)
    {
        playback.ReleaseInteraction();
        base.OnLostMouseCapture(args);
    }

    internal SceneNode? Find(Point at)
    {
        if (Viewport is null) return null;
        foreach (var hit in Viewport3DHelper.FindHits(Viewport, at))
            if (hit.Visual is { } visual && picks.Find(visual) is { } id &&
                nodes.TryGetValue(id, out var found)) return found.Node;
        return null;
    }

    private void SetHover(SceneNode? node) => hover.Observe(node);
}
