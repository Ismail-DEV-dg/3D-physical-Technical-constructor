using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.App.Controls;

public sealed class BreadboardSurface : FrameworkElement
{
    private readonly PinHoverOverlay hover=new();
    protected override int VisualChildrenCount=>1;
    protected override Visual GetVisualChild(int index)=>index==0?hover:throw new ArgumentOutOfRangeException(nameof(index));
    internal string? HoveredContact=>hover.ActiveKey;
    private const double MinimumCullingPaddingMm = 6;
    private readonly ComponentDrawingCache components = new();
    private readonly BreadboardDrawingCache breadboard = new();
    private readonly WireDrawingCache wires = new();
    private readonly ToolTip tip = new() { Placement = PlacementMode.MousePoint, IsHitTestVisible = false, Focusable = false };
    public static readonly DependencyProperty SceneProperty = DependencyProperty.Register(nameof(Scene),
        typeof(BreadboardViewModel), typeof(BreadboardSurface), new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.AffectsRender, (owner, _) => ((BreadboardSurface)owner).ClearDrawings()));
    public static readonly DependencyProperty RevisionProperty = DependencyProperty.Register(nameof(Revision),
        typeof(int), typeof(BreadboardSurface), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender,(owner,_)=>((BreadboardSurface)owner).RefreshHover()));
    public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(nameof(Background),
        typeof(Brush), typeof(BreadboardSurface), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty SimulationProperty = DependencyProperty.Register(nameof(Simulation),
        typeof(SimulationState), typeof(BreadboardSurface), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty ShowVoltageProperty = DependencyProperty.Register(nameof(ShowVoltage),
        typeof(bool), typeof(BreadboardSurface), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty ShowCurrentProperty = DependencyProperty.Register(nameof(ShowCurrent),
        typeof(bool), typeof(BreadboardSurface), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
    public SimulationState? Simulation { get => (SimulationState?)GetValue(SimulationProperty); set => SetValue(SimulationProperty, value); }
    public bool ShowVoltage { get => (bool)GetValue(ShowVoltageProperty); set => SetValue(ShowVoltageProperty, value); }
    public bool ShowCurrent { get => (bool)GetValue(ShowCurrentProperty); set => SetValue(ShowCurrentProperty, value); }
    public BreadboardViewModel? Scene { get => (BreadboardViewModel?)GetValue(SceneProperty); set => SetValue(SceneProperty, value); }
    public int Revision { get => (int)GetValue(RevisionProperty); set => SetValue(RevisionProperty, value); }
    public Brush Background { get => (Brush)GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
    public BreadboardSurface() {AddVisualChild(hover);Unloaded += (_, _) => { tip.IsOpen = false; hover.Update(null);ClearDrawings(); };}
    private void ClearDrawings() { hover.Update(null);components.Clear(); breadboard.Clear(); }
    public string? GetPinToolTip(Point point)
    {
        if (Scene is not { } scene) return null;
        var x = PhysicalSettings.ToMillimeters((point.X - scene.OffsetX) / scene.Zoom);
        var y = PhysicalSettings.ToMillimeters((point.Y - scene.OffsetY) / scene.Zoom);
        return PhysicalPinToolTips.Find(scene, x, y);
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (ActualWidth <= 0 || ActualHeight <= 0 || Scene is not { } scene) return;
        hover.Clip=new RectangleGeometry(new Rect(RenderSize));
        dc.PushClip(new RectangleGeometry(new Rect(RenderSize)));
        dc.DrawRectangle(Background, null, new Rect(RenderSize));
        var scale = scene.Zoom * PhysicalSettings.PixelsPerMillimeter;
        dc.PushTransform(new MatrixTransform(scale, 0, 0, scale, scene.OffsetX, scene.OffsetY));
        var viewport = new Rect(RenderSize);
        var pitch = scene.Board?.Definition.HolePitch ?? PhysicalSettings.FreeGridMm;
        if (scene.Board is { } board)
            breadboard.Draw(dc, board, scene);
        foreach (var item in scene.Project.Components)
        {
            var pose = scene.PlacementOf(item);
            var bounds = ContactGeometry.Bounds(item.Footprint, pose, pitch);
            var visibleBounds = new Rect((pose.X + bounds.X) * scale + scene.OffsetX,
                (pose.Y + bounds.Y) * scale + scene.OffsetY, bounds.Width * scale, bounds.Height * scale);
            // Include lead rings and the selected reference above the body, at every rotation.
            var padding = Math.Max(MinimumCullingPaddingMm,
                item.IsSelected ? item.Reference.Length * RealComponentDrawing.ReferenceFontSizeMm : 0) * scale;
            visibleBounds.Inflate(padding, padding);
            if (!viewport.IntersectsWith(visibleBounds)) continue;
            dc.PushTransform(new TranslateTransform(pose.X, pose.Y));
            dc.PushTransform(new RotateTransform(pose.Rotation));
            dc.DrawDrawing(components.Get(item, pose, pitch));
            var signal = Simulation?.Components.GetValueOrDefault(item.Model.Id);
            SimulationDrawing.Component(dc, item, signal);
            RealComponentDrawing.DrawSelection(dc, item, pose, pitch,
                scene.Preview is { Success: false } && item.IsSelected, signal?.Angle);
            dc.Pop(); dc.Pop();
        }
        WireDrawing.Draw(dc, scene, simulation: Simulation, showVoltage: ShowVoltage, showCurrent: ShowCurrent, cache: wires,
            viewport: new(-scene.OffsetX / scale, -scene.OffsetY / scale, RenderSize.Width / scale, RenderSize.Height / scale));
        AnalysisDrawing.Draw(dc,scene.Project,true);
        VoltageDrawing.Draw(dc,scene.Project,true);
        dc.Pop(); dc.Pop();
    }
    private void RefreshHover()
    {
        if(IsMouseOver && Mouse.LeftButton==MouseButtonState.Released && Mouse.RightButton==MouseButtonState.Released && Mouse.MiddleButton==MouseButtonState.Released)UpdateContactHover(Mouse.GetPosition(this));
        else hover.Update(null);
    }
    internal void UpdateContactHover(Point point)
    {
        if(Scene is not { } scene){hover.Update(null);return;}
        var world=new Core.Layout.LayoutPoint(PhysicalSettings.ToMillimeters((point.X-scene.OffsetX)/scene.Zoom),PhysicalSettings.ToMillimeters((point.Y-scene.OffsetY)/scene.Zoom));
        var target=PhysicalContactHover.Find(scene,world);
        if(target is { } t)hover.Update(t.Key,new Point(PhysicalSettings.ToPixels(t.Position.X)*scene.Zoom+scene.OffsetX,PhysicalSettings.ToPixels(t.Position.Y)*scene.Zoom+scene.OffsetY),Math.Max(3,PhysicalSettings.ToPixels(.7)*scene.Zoom));
        else hover.Update(null);
    }
    protected override void OnMouseMove(MouseEventArgs args)
    {
        base.OnMouseMove(args);
        if (Scene is not { } scene || args.LeftButton != MouseButtonState.Released
            || args.RightButton != MouseButtonState.Released || args.MiddleButton != MouseButtonState.Released)
        { hover.Update(null);tip.IsOpen = false; return; }
        var point = args.GetPosition(this);
        UpdateContactHover(point);
        var x = PhysicalSettings.ToMillimeters((point.X - scene.OffsetX) / scene.Zoom);
        var y = PhysicalSettings.ToMillimeters((point.Y - scene.OffsetY) / scene.Zoom);
        var item = scene.Project.Components.LastOrDefault(item => scene.Hit(item, x, y));
        string? text = GetPinToolTip(point) ?? (item is null ? null : $"{item.Reference} · {item.Name}\n{item.Model.Value}" +
            (item.Footprint.Renderer == "placeholder" ? "\nВременная заглушка корпуса." : ""));
        if (text is null && scene.Board is { } board)
        {
            var hole = board.Definition.Holes.FirstOrDefault(hole =>
                Math.Abs(Core.Breadboards.BreadboardGeometry.World(board,hole.Position).X - x) <= PhysicalSettings.HoleSnapToleranceMm
                && Math.Abs(Core.Breadboards.BreadboardGeometry.World(board,hole.Position).Y - y) <= PhysicalSettings.HoleSnapToleranceMm);
            if (hole is not null)
            {
                var occupied = scene.Project.Model.Bindings.Any(binding => binding.BoardId == board.Id && binding.HoleId == hole.Id);
                text = $"{hole.Id}\nГруппа: {hole.GroupId}\n{(occupied ? "Занято" : "Свободно")}";
            }
        }
        if (text is null) { tip.IsOpen = false; return; }
        text+=item is not null ? "\nR — поворот · Del — удалить · Ctrl+D — копия\nW — перемычки · V / Esc — перемещение · F1 — справка"
            : "\nW — перемычки · край платы: перетащить, R — повернуть · F1 — справка";
        tip.PlacementTarget = this;
        if (!Equals(tip.Content, text)) tip.Content = text;
        if (!tip.IsOpen) tip.IsOpen = true;
    }
    protected override void OnMouseLeave(MouseEventArgs args) { hover.Update(null);tip.IsOpen = false; base.OnMouseLeave(args); }
    protected override void OnMouseDown(MouseButtonEventArgs args) { hover.Update(null);tip.IsOpen = false; base.OnMouseDown(args); }
}
