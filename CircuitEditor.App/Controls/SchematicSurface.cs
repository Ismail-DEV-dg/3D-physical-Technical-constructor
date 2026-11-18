using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Geometry;
using CircuitEditor.Core.Layout;

namespace CircuitEditor.App.Controls;

// Rendering and presentation-only tooltips; document edits remain in ViewModels.
public sealed class SchematicSurface : FrameworkElement
{
    private readonly PinHoverOverlay hover=new();
    protected override int VisualChildrenCount=>1;
    protected override Visual GetVisualChild(int index)=>index==0?hover:throw new ArgumentOutOfRangeException(nameof(index));
    private const double PinRadius = 3;
    private const double PinHitRadius = 8;
    private readonly ToolTip pinTip = new()
    {
        Placement = PlacementMode.MousePoint, Focusable = false, IsHitTestVisible = false
    };
    public static readonly DependencyProperty SceneProperty = DependencyProperty.Register(
        nameof(Scene), typeof(SchematicViewModel), typeof(SchematicSurface),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender,(o,_)=>((SchematicSurface)o).RefreshHover()));
    public static readonly DependencyProperty RevisionProperty = DependencyProperty.Register(
        nameof(Revision), typeof(int), typeof(SchematicSurface),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender,(o,_)=>((SchematicSurface)o).RefreshHover()));
    public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
        nameof(Background), typeof(Brush), typeof(SchematicSurface),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));
    public SchematicViewModel? Scene { get => (SchematicViewModel?)GetValue(SceneProperty); set => SetValue(SceneProperty, value); }
    public int Revision { get => (int)GetValue(RevisionProperty); set => SetValue(RevisionProperty, value); }
    public Brush Background { get => (Brush)GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }

    public SchematicSurface(){AddVisualChild(hover);Unloaded += (_, _) => {hover.Update(null);pinTip.IsOpen=false;};}

    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        var viewport = new Rect(RenderSize);
        hover.Clip=new RectangleGeometry(viewport);
        context.PushClip(new RectangleGeometry(viewport));
        context.DrawRectangle(Background, null, viewport);
        if (Scene is not { } scene) { context.Pop(); return; }
        DrawGrid(context, scene);
        context.PushTransform(new MatrixTransform(scene.Zoom, 0, 0, scene.Zoom, scene.OffsetX, scene.OffsetY));
        SchematicConnectionsDrawing.Draw(context,scene.Project);
        foreach (var item in scene.Components) DrawComponent(context, item);
        AnalysisDrawing.Draw(context,scene.Project,false);
        VoltageDrawing.Draw(context,scene.Project,false);
        if (scene.IsSelecting)
        {
            var fill = Brush("AccentBrush").Clone(); fill.Opacity = 0.12;
            context.DrawRectangle(fill, new Pen(Brush("AccentBrush"), 1 / scene.Zoom),
                new Rect(scene.SelectionLeft, scene.SelectionTop, scene.SelectionWidth, scene.SelectionHeight));
        }
        context.Pop();
        if (scene.ComponentCount == 0)
        {
            context.DrawText(WpfTextMeasurer.Format("Перетащите компонент из палитры", 14, Brush("MutedBrush")), new Point(24, 24));
            context.DrawText(WpfTextMeasurer.Format("Колесо — масштаб  ·  СКМ / ПКМ — панорама", 12, Brush("MutedBrush")), new Point(24, 50));
        }
        context.Pop();
    }

    private void DrawGrid(DrawingContext context, SchematicViewModel scene)
    {
        var step = DiagramGeometry.GridSize * scene.Zoom;
        var pen = new Pen(Brush("GridBrush"), 0.6);
        for (var x = scene.OffsetX % step; x < ActualWidth; x += step)
            context.DrawLine(pen, new Point(x, 0), new Point(x, ActualHeight));
        for (var y = scene.OffsetY % step; y < ActualHeight; y += step)
            context.DrawLine(pen, new Point(0, y), new Point(ActualWidth, y));
        var axes = new Pen(Brush("LineBrush"), 1);
        if (scene.OffsetX >= 0 && scene.OffsetX <= ActualWidth)
            context.DrawLine(axes, new Point(scene.OffsetX, 0), new Point(scene.OffsetX, ActualHeight));
        if (scene.OffsetY >= 0 && scene.OffsetY <= ActualHeight)
            context.DrawLine(axes, new Point(0, scene.OffsetY), new Point(ActualWidth, scene.OffsetY));
    }

    internal void DrawComponent(DrawingContext context, ComponentViewModel item, bool selection = true)
    {
        var model = item.Model;
        var layout = item.Layout;
        context.PushTransform(new TranslateTransform(item.SchematicPlacement.X, item.SchematicPlacement.Y));
        context.PushTransform(new RotateTransform(item.SchematicPlacement.Rotation, layout.Width / 2, layout.Height / 2));
        var ink = new SolidColorBrush((Color)ColorConverter.ConvertFromString(model.Color ?? model.Definition.DefaultColor));
        var body = Rect(layout.Body);
        if (selection && item.IsSelected)
        {
            var glow = Brush("AccentBrush").Clone(); glow.Opacity = 0.18;
            context.DrawRoundedRectangle(null, new Pen(glow, 10), body, 7, 7);
            context.DrawRoundedRectangle(null, new Pen(Brush("AccentBrush"), 3), body, 7, 7);
        }
        context.DrawRoundedRectangle(Brush("PanelBrush"), new Pen(ink, 1.3), body, 6, 6);
        SymbolDrawing.Draw(context, model.Definition.Symbol, ink, Rect(layout.Symbol));
        foreach (var text in layout.Texts) DrawText(context, text, Brush("TextBrush"));
        foreach (var pin in layout.Pins)
        {
            var pinInk = pin.Pin.IsInputOnly ? Brush("PurpleBrush") : ink;
            var endX = pin.IsLeft ? layout.Body.X : layout.Body.Right;
            context.DrawLine(new Pen(pinInk, 1.4), new Point(pin.Position.X, pin.Position.Y),
                new Point(endX, pin.Position.Y));
            context.DrawEllipse(Brush("BackgroundBrush"), new Pen(pinInk, 1.2),
                new Point(pin.Position.X, pin.Position.Y), PinRadius, PinRadius);
            DrawText(context, pin.Label, pin.Pin.IsInputOnly ? Brush("PurpleBrush") : Brush("TextBrush"));
        }
        context.Pop(); context.Pop();
    }

    private static void DrawText(DrawingContext context, LayoutText text, Brush brush)
    {
        // Same font, size, formatting mode and ink padding as the measuring adapter.
        // No MaxTextWidth, ellipsis, clipping rectangle or font-size reduction.
        var formatted = WpfTextMeasurer.Format(text.Text, text.FontSize, brush);
        context.DrawText(formatted, new Point(text.Bounds.X + WpfTextMeasurer.InkPadding,
            text.Bounds.Y + WpfTextMeasurer.InkPadding));
    }

    protected override void OnMouseMove(MouseEventArgs args)
    {
        base.OnMouseMove(args);
        UpdateHover(args.GetPosition(this));UpdatePinTip(args);
    }
    private void RefreshHover()
    {if(IsMouseOver)UpdateHover(Mouse.GetPosition(this));else hover.Update(null);}
    private void UpdateHover(Point pointer)
    {
        string? key=null;Point location=default;var radius=4d;
        if(Scene is { } scene && Mouse.LeftButton==MouseButtonState.Released && Mouse.RightButton==MouseButtonState.Released && Mouse.MiddleButton==MouseButtonState.Released)
        {
            foreach(var item in scene.Components.Reverse())
            {
                var target=item.Layout.Pins.Select(p=>(p.Pin.Id,Point:SchematicConnectionsDrawing.Position(scene.Project,new(item.Model.Id,p.Pin.Id))))
                    .FirstOrDefault(p=>p.Point is { } q && Math.Pow(q.X*scene.Zoom+scene.OffsetX-pointer.X,2)+Math.Pow(q.Y*scene.Zoom+scene.OffsetY-pointer.Y,2)<=PinHitRadius*PinHitRadius);
                if(target.Point is { } point){key=$"{item.Model.Id}:{target.Id}";location=new Point(point.X*scene.Zoom+scene.OffsetX,point.Y*scene.Zoom+scene.OffsetY);radius=Math.Max(4,4*scene.Zoom);break;}
            }
        }
        hover.Update(key,location,radius);
    }
    protected override void OnMouseUp(MouseButtonEventArgs args)
    {base.OnMouseUp(args);UpdateHover(args.GetPosition(this));UpdatePinTip(args);}

    protected override void OnMouseLeave(MouseEventArgs args)
    {
        base.OnMouseLeave(args);
        hover.Update(null);pinTip.IsOpen = false;
    }

    protected override void OnMouseDown(MouseButtonEventArgs args)
    {
        hover.Update(null);pinTip.IsOpen = false;
        base.OnMouseDown(args);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs args)
    {
        hover.Update(null);pinTip.IsOpen = false;
        base.OnMouseWheel(args);
    }

    private void UpdatePinTip(MouseEventArgs args)
    {
        var text = args.LeftButton == MouseButtonState.Released && args.RightButton == MouseButtonState.Released
            && args.MiddleButton == MouseButtonState.Released ? GetPinToolTip(args.GetPosition(this)) : null;
        if(text is null && Scene is { } scene && args.LeftButton==MouseButtonState.Released && args.RightButton==MouseButtonState.Released && args.MiddleButton==MouseButtonState.Released)
        {
            var point=args.GetPosition(this);var x=(point.X-scene.OffsetX)/scene.Zoom;var y=(point.Y-scene.OffsetY)/scene.Zoom;
            var item=scene.Components.LastOrDefault(c=>DiagramGeometry.Contains(c.SchematicPlacement,x,y,c.Layout.Width,c.Layout.Height));
            if(item is not null)text=item.Reference+" · "+item.Name;
        }
        if (text is null) { pinTip.IsOpen = false; return; }
        pinTip.PlacementTarget = this;
        pinTip.Content = text+"\nR — поворот · Del — удалить · Ctrl+D — копия · F1 — справка";
        pinTip.IsOpen = true;
    }

    public string? GetPinToolTip(Point viewportPoint)
    {
        if (Scene is not { } scene) return null;
        var world = new LayoutPoint((viewportPoint.X - scene.OffsetX) / scene.Zoom,
            (viewportPoint.Y - scene.OffsetY) / scene.Zoom);
        foreach (var item in scene.Components.Reverse())
        {
            var local = CircuitEditor.Core.Layout.LayoutTransform.ToLocal(item.SchematicPlacement, item.Layout, world);
            var radius = PinHitRadius / scene.Zoom;
            double Distance(PinLayout pin) => Math.Pow(local.X - pin.Position.X, 2) + Math.Pow(local.Y - pin.Position.Y, 2);
            var pin = item.Layout.Pins.FirstOrDefault(candidate => candidate.Label.Bounds.Contains(local))
                ?? item.Layout.Pins.Where(candidate => Distance(candidate) <= radius * radius).OrderBy(Distance).FirstOrDefault();
            if (pin is not null)
            {
                var functions = string.Join(", ", pin.Pin.Capabilities.Prepend(pin.Pin.Type).Distinct());
                var direction = pin.Pin.IsInputOnly
                    ? "input only — только вход; отмечен фиолетовым."
                    : "Направление определяется функцией вывода.";
                var polarity = pin.Pin.Id == "RESET"
                    ? "\nactive-low — сброс активен при низком уровне (0)."
                    : pin.Pin.Id == "EN" ? "\nEN — разрешение работы; низкий уровень отключает чип." : "";
                return $"{item.Reference} · {pin.Pin.Name}\n{functions}\n{direction}{polarity}" +
                    "\nЛегенда: фиолетовый — input only; active-low — активный уровень 0.";
            }
        }
        return null;
    }

    private Brush Brush(string key) => (Brush?)TryFindResource(key) ?? Brushes.Gray;
    private static Rect Rect(LayoutRect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);
}
