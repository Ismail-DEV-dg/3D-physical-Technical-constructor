using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.App.Controls;

public sealed class BreadboardSurface : FrameworkElement
{
    private const double MinimumCullingPaddingMm = 4;
    private readonly ComponentDrawingCache components = new();
    private readonly BreadboardDrawingCache breadboard = new();
    private readonly ToolTip tip = new() { Placement = PlacementMode.MousePoint, IsHitTestVisible = false, Focusable = false };
    public static readonly DependencyProperty SceneProperty = DependencyProperty.Register(nameof(Scene),
        typeof(BreadboardViewModel), typeof(BreadboardSurface), new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.AffectsRender, (owner, _) => ((BreadboardSurface)owner).ClearDrawings()));
    public static readonly DependencyProperty RevisionProperty = DependencyProperty.Register(nameof(Revision),
        typeof(int), typeof(BreadboardSurface), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(nameof(Background),
        typeof(Brush), typeof(BreadboardSurface), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));
    public BreadboardViewModel? Scene { get => (BreadboardViewModel?)GetValue(SceneProperty); set => SetValue(SceneProperty, value); }
    public int Revision { get => (int)GetValue(RevisionProperty); set => SetValue(RevisionProperty, value); }
    public Brush Background { get => (Brush)GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
    public BreadboardSurface() => Unloaded += (_, _) => { tip.IsOpen = false; ClearDrawings(); };
    private void ClearDrawings() { components.Clear(); breadboard.Clear(); }
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
        dc.PushClip(new RectangleGeometry(new Rect(RenderSize)));
        dc.DrawRectangle(Background, null, new Rect(RenderSize));
        var scale = scene.Zoom * PhysicalSettings.PixelsPerMillimeter;
        dc.PushTransform(new MatrixTransform(scale, 0, 0, scale, scene.OffsetX, scene.OffsetY));
        var viewport = new Rect(RenderSize);
        var pitch = scene.Board?.Definition.HolePitch ?? PhysicalSettings.FreeGridMm;
        if (scene.Board is { } board && viewport.IntersectsWith(new Rect(board.X * scale + scene.OffsetX,
            board.Y * scale + scene.OffsetY, board.Definition.Width * scale, board.Definition.Height * scale)))
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
            RealComponentDrawing.DrawSelection(dc, item, pose, pitch,
                scene.Preview is { Success: false } && item.IsSelected);
            dc.Pop(); dc.Pop();
        }
        dc.Pop(); dc.Pop();
    }
    protected override void OnMouseMove(MouseEventArgs args)
    {
        base.OnMouseMove(args);
        if (Scene is not { } scene || args.LeftButton != MouseButtonState.Released
            || args.RightButton != MouseButtonState.Released || args.MiddleButton != MouseButtonState.Released)
        { tip.IsOpen = false; return; }
        var point = args.GetPosition(this);
        var x = PhysicalSettings.ToMillimeters((point.X - scene.OffsetX) / scene.Zoom);
        var y = PhysicalSettings.ToMillimeters((point.Y - scene.OffsetY) / scene.Zoom);
        var item = scene.Project.Components.LastOrDefault(item => scene.Hit(item, x, y));
        string? text = GetPinToolTip(point) ?? (item is null ? null : $"{item.Reference} · {item.Name}\n{item.Model.Value}" +
            (item.Footprint.Renderer == "placeholder" ? "\nВременная заглушка корпуса." : ""));
        if (text is null && scene.Board is { } board)
        {
            var hole = board.Definition.Holes.FirstOrDefault(hole =>
                Math.Abs(hole.Position.X + board.X - x) <= PhysicalSettings.HoleSnapToleranceMm
                && Math.Abs(hole.Position.Y + board.Y - y) <= PhysicalSettings.HoleSnapToleranceMm);
            if (hole is not null)
            {
                var occupied = scene.Project.Model.Bindings.Any(binding => binding.BoardId == board.Id && binding.HoleId == hole.Id);
                text = $"{hole.Id}\nГруппа: {hole.GroupId}\n{(occupied ? "Занято" : "Свободно")}";
            }
        }
        if (text is null) { tip.IsOpen = false; return; }
        tip.PlacementTarget = this;
        if (!Equals(tip.Content, text)) tip.Content = text;
        if (!tip.IsOpen) tip.IsOpen = true;
    }
    protected override void OnMouseLeave(MouseEventArgs args) { tip.IsOpen = false; base.OnMouseLeave(args); }
    protected override void OnMouseDown(MouseButtonEventArgs args) { tip.IsOpen = false; base.OnMouseDown(args); }
}
