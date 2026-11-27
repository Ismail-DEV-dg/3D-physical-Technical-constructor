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
    private readonly ComponentGeometryCache geometry = new();
    private readonly BreadboardDrawingCache breadboard = new();
    private readonly WireDrawingCache wires = new();
    private readonly ToolTip tip = new() { Placement = PlacementMode.MousePoint, IsHitTestVisible = false, Focusable = false };
    private Point? splitterHover;
    public static readonly DependencyProperty SceneProperty = DependencyProperty.Register(nameof(Scene),
        typeof(BreadboardViewModel), typeof(BreadboardSurface), new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.AffectsRender, (owner, _) => ((BreadboardSurface)owner).ClearDrawings()));
    public static readonly DependencyProperty RevisionProperty = DependencyProperty.Register(nameof(Revision),
        typeof(int), typeof(BreadboardSurface), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender,(owner,_)=>((BreadboardSurface)owner).RefreshHover()));
    public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(nameof(Background),
        typeof(Brush), typeof(BreadboardSurface), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty SimulationProperty = DependencyProperty.Register(nameof(Simulation),
        typeof(SimulationState), typeof(BreadboardSurface), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender,
            (owner, _) => ((BreadboardSurface)owner).RefreshSplitterToolTip()));
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
    public string? GetPinToolTip(Point point) => GetPinToolTip(point, out _);

    private string? GetPinToolTip(Point point, out bool splitter)
    {
        splitter = false;
        if (Scene is not { } scene) return null;
        var x = PhysicalSettings.ToMillimeters((point.X - scene.OffsetX) / scene.Zoom);
        var y = PhysicalSettings.ToMillimeters((point.Y - scene.OffsetY) / scene.Zoom);
        return PhysicalPinToolTips.Find(scene, x, y, out splitter);
    }

    private void RefreshSplitterToolTip()
    {
        if (!tip.IsOpen || splitterHover is not { } point) return;
        if (GetPinToolTip(point, out var isSplitter) is { } description && isSplitter)
        {
            if (!Equals(description, tip.Content)) tip.Content = description;
            return;
        }
        splitterHover = null;
        tip.IsOpen = false;
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
        var viewport = new Core.Layout.LayoutRect(-scene.OffsetX / scale, -scene.OffsetY / scale,
            RenderSize.Width / scale, RenderSize.Height / scale);
        var pitch = scene.Board?.Definition.HolePitch ?? PhysicalSettings.FreeGridMm;
        if (scene.Board is { } board)
            breadboard.Draw(dc, board, scene);
        foreach (var item in scene.Project.Components)
        {
            var pose = scene.PlacementOf(item);
            var drawing = geometry.Get(item.Footprint, pose, pitch);
            var bounds = drawing.Bounds;
            // Include lead rings and the selected reference above the body, at every rotation.
            var padding = Math.Max(MinimumCullingPaddingMm,
                item.IsSelected ? (item.Reference.Length + item.Model.Value.Length + 2) * RealComponentDrawing.ReferenceFontSizeMm : 0);
            if (!viewport.Intersects(new(bounds.X - padding, bounds.Y - padding,
                bounds.Width + 2 * padding, bounds.Height + 2 * padding))) continue;
            dc.PushTransform(drawing.Transform);
            dc.DrawDrawing(components.Get(item, pose, pitch));
            var signal = Simulation?.Components.GetValueOrDefault(item.Model.Id);
            SimulationDrawing.Component(dc, item, signal);
            RealComponentDrawing.DrawSelection(dc, item, pose, pitch,
                scene.Preview is { Success: false } && item.IsSelected, signal?.Angle);
            dc.Pop();
        }
        WireDrawing.Draw(dc, scene, simulation: Simulation, showVoltage: ShowVoltage, showCurrent: ShowCurrent, cache: wires,
            viewport: viewport);
        if (scene.PalettePreview is { } preview)
            PalettePreviewDrawing.Draw(dc, preview, scene.Board, pitch);
        else if (scene.HeldPlacementPreview is { } held)
            PalettePreviewDrawing.Draw(dc, held, scene.Board, pitch);
        if (scene.IsNetInspectionActive)
            NetInspectionDrawing.Draw(dc, scene, viewport, wires, TryFindResource("WarningBrush") as Brush ?? Brushes.Gold);
        foreach (var note in scene.Project.Model.MockupNotes)
            PhysicalDrawing.Text(dc, note.Text, note.X, note.Y, 2.3, PhysicalDrawing.Ink("#EAF3F1"));
        if (scene.RulerStart is { } start && scene.RulerEnd is { } end)
        {
            var ink = PhysicalDrawing.Ink("#FFD47A");
            dc.DrawLine(new Pen(ink, .4) { DashStyle = DashStyles.Dash }, new Point(start.X, start.Y), new Point(end.X, end.Y));
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            PhysicalDrawing.Text(dc, $"{Math.Sqrt(dx * dx + dy * dy):0.##} мм", end.X + 1.5, end.Y - 4, 2.8, ink);
        }
        AnalysisDrawing.Draw(dc,scene.Project,true);
        VoltageDrawing.Draw(dc,scene.Project,true);
        dc.Pop(); dc.Pop();
    }
    private void RefreshHover()
    {
        if (splitterHover is not null) RefreshSplitterToolTip();
        else if (Scene?.Project.Wiring.HoveredId is not null) tip.IsOpen = false;
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
        { splitterHover = null; hover.Update(null);tip.IsOpen = false; return; }
        var point = args.GetPosition(this);
        UpdateContactHover(point);
        var pinText = GetPinToolTip(point, out var isSplitter);
        splitterHover = isSplitter ? point : null;
        if (scene.Project.Wiring.HoveredId is not null && !isSplitter) { tip.IsOpen = false; return; }
        var x = PhysicalSettings.ToMillimeters((point.X - scene.OffsetX) / scene.Zoom);
        var y = PhysicalSettings.ToMillimeters((point.Y - scene.OffsetY) / scene.Zoom);
        var item = scene.Project.Components.LastOrDefault(item => scene.Hit(item, x, y));
        string? text = pinText ?? (item is null ? null : $"{item.Reference} · {item.Name}\n{item.Model.Value}" +
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
        if (!isSplitter) text+=item is not null ? "\nR — поворот · Del — удалить · Ctrl+D — копия\nW — перемычки · V / Esc — перемещение · F1 — справка"
            : "\nW — перемычки · край платы: перетащить, R — повернуть · F1 — справка";
        tip.PlacementTarget = this;
        if (!Equals(tip.Content, text)) tip.Content = text;
        if (!tip.IsOpen) tip.IsOpen = true;
    }
    protected override void OnMouseLeave(MouseEventArgs args)
    {
        splitterHover = null;
        hover.Update(null);
        tip.IsOpen = false;
        if (Scene is { } scene && scene.Project.Wiring.HoveredId is not null)
        {
            scene.Project.Wiring.HoveredId = null;
            scene.Revision++;
        }
        base.OnMouseLeave(args);
    }
    protected override void OnMouseDown(MouseButtonEventArgs args) { splitterHover = null; hover.Update(null);tip.IsOpen = false; base.OnMouseDown(args); }
}
