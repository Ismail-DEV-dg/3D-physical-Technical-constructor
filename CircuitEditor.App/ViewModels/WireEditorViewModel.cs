using CircuitEditor.App.Interaction;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Wiring;
using CommunityToolkit.Mvvm.ComponentModel;
namespace CircuitEditor.App.ViewModels;
public sealed partial class WireEditorViewModel(ProjectViewModel project) : ObservableObject
{
    private const double PickRadiusMm = 1.3;
    private WireEndpoint? first;
    private Guid? selectedId;
    private int? draggingNode;
    private Wire? original;
    private bool invalidEnd;
    [ObservableProperty] private bool isWiring;
    partial void OnIsWiringChanged(bool value)
    {
        if(!value){Cancel();selectedId=null;HoveredId=null;Refresh();}
        project.InvalidatePresentation();
    }
    public void Exit(){IsWiring=false;Cancel();selectedId=null;Refresh();}
    [ObservableProperty] private Guid? hoveredId;
    [ObservableProperty] private LayoutPoint pointer;
    public Wire? Selected => project.Model.Circuit.Wires.FirstOrDefault(w => w.Id == selectedId);
    public bool HasSelection => Selected is not null;
    public LayoutPoint? DraggedStart => draggingNode == -1 ? Pointer : null;
    public LayoutPoint? DraggedEnd => draggingNode is not null && Selected is { } w && draggingNode == w.ControlPoints.Count ? Pointer : null;
    public WireEndpoint? PreviewStart => first;
    public IReadOnlyList<string> Colors => WirePalette.Colors;
    private string color = WirePalette.Colors[0];
    public string Color
    {
        get => Selected?.Color ?? color;
        set
        {
            if (!Colors.Contains(value)) return;
            color = value;
            if (Selected is { } wire && wire.Color != value)
            {
                project.Model.Circuit.PutWire(new Wire(wire.Id, wire.Start, wire.End, value, wire.ControlPoints));
                project.Refresh();
            }
            OnPropertyChanged();
        }
    }
    public void Refresh() { OnPropertyChanged(nameof(Selected)); OnPropertyChanged(nameof(HasSelection)); OnPropertyChanged(nameof(Color)); }
    public bool Handle(CanvasInput input, LayoutPoint point)
    {
        if (input.Pan) return false;
        if (input.Action == CanvasAction.Cancel && (first is not null || draggingNode is not null))
        { Cancel(); return true; }
        if (input.Action == CanvasAction.Delete && Selected is { } deleted)
        {
            project.Model.Circuit.RemoveWire(deleted.Id); selectedId = null; project.Refresh(); Refresh(); return true;
        }
        if (input.Action == CanvasAction.Move)
        {
            if (draggingNode is not null && original is not null) { Drag(point); return true; }
            if (first is not null) { Pointer = point; return true; }
            var hover = Hit(point)?.Id;
            if (hover != HoveredId) { HoveredId = hover; return true; }
            return false;
        }
        if (input.Action == CanvasAction.Up && draggingNode is not null)
        {
            Drag(point);
            var error = invalidEnd ? "Конец должен находиться на контакте или отверстии." : Selected is { } wire ? WireGeometry.Validate(project.Model, wire) : "Перемычка удалена.";
            draggingNode = null; original = null; invalidEnd = false;
            if (error is not null) { project.CancelGesture(); project.StatusMessage = error; }
            else project.EndGesture();
            project.Refresh(); Refresh(); return true;
        }
        if (input.Action != CanvasAction.Down) return false;
        if (first is not null)
        {
            var end = WireGeometry.Nearest(project.Model, point);
            if (end is null) { project.StatusMessage = "Выберите контакт или отверстие; Esc — отмена."; return true; }
            try
            {
                var wire = new Wire(first, end, color);
                if (WireGeometry.Validate(project.Model, wire) is { } error) { project.StatusMessage = error; return true; }
                project.Model.Circuit.PutWire(wire); first = null; selectedId = wire.Id;
                project.Refresh(); Refresh(); project.StatusMessage = "Перемычка создана.";
            }
            catch (ArgumentException e) { project.StatusMessage = e.Message; }
            return true;
        }
        if (Selected is { } selected)
        {
            var points = Points(selected);
            for (var i = 0; i < points.Count; i++)
                if (Distance(points[i], point) <= PickRadiusMm)
                { BeginDrag(selected, i - 1); return true; }
        }
        var hit = Hit(point);
        if (hit is not null)
        {
            var alreadySelected = selectedId == hit.Id;
            selectedId = hit.Id; project.Select(null); Refresh();
            if (alreadySelected)
            {
                project.BeginGesture("Изгиб перемычки"); original = hit;
                var nodes = hit.ControlPoints.ToList();
                draggingNode = WireCurve.FindInsertionIndex(Points(hit), point);
                nodes.Insert(draggingNode.Value, point);
                project.Model.Circuit.PutWire(new Wire(hit.Id, hit.Start, hit.End, hit.Color, nodes)); project.Refresh();
            }
            return true;
        }
        if (IsWiring && WireGeometry.Nearest(project.Model, point) is { } endpoint)
        { first = endpoint; Pointer = point; project.StatusMessage = "Выберите второй конец; Esc — отмена."; return true; }
        selectedId = null; Refresh(); return false;
    }
    public void Cancel()
    { first = null; draggingNode = null; original = null; project.CancelGesture(); Refresh(); }
    private void BeginDrag(Wire wire, int index)
    { original = wire; draggingNode = index; Pointer = Points(wire)[index+1]; project.BeginGesture("Изменение перемычки"); }
    private void Drag(LayoutPoint point)
    {
        Pointer = point;
        var wire = Selected; if (wire is null || original is null) return;
        var start = wire.Start; var end = wire.End; var nodes = wire.ControlPoints.ToArray();
        if (draggingNode == -1 || draggingNode == nodes.Length)
        {
            var endpoint = WireGeometry.Nearest(project.Model, point);
            invalidEnd = endpoint is null;
            if (endpoint is null) { Pointer = point; return; }
            if (draggingNode == -1) start = endpoint; else end = endpoint;
            if (start == end) { invalidEnd = true; project.StatusMessage = "Нулевая длина перемычки."; return; }
        }
        else nodes[draggingNode!.Value] = point;
        try { project.Model.Circuit.PutWire(new Wire(wire.Id, start, end, wire.Color, nodes)); project.Refresh(); }
        catch (ArgumentException e) { project.StatusMessage = e.Message; }
    }
    public IReadOnlyList<LayoutPoint> Points(Wire wire)
    {
        var a = WireGeometry.Resolve(project.Model, wire.Start); var b = WireGeometry.Resolve(project.Model, wire.End);
        return a is null || b is null ? [] : new[] { a.Value }.Concat(wire.ControlPoints).Append(b.Value).ToArray();
    }
    private Wire? Hit(LayoutPoint point) => project.Model.Circuit.Wires.Reverse().FirstOrDefault(w =>
    {
        var points = WireCurve.Sample(Points(w));
        return points.Zip(points.Skip(1)).Any(p => SegmentDistance(point, p.First, p.Second) <= PickRadiusMm);
    });
    private static double SegmentDistance(LayoutPoint p, LayoutPoint a, LayoutPoint b)
    {
        var dx = b.X - a.X; var dy = b.Y - a.Y; var squared = dx * dx + dy * dy;
        var t = squared == 0 ? 0 : Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / squared, 0, 1);
        return Distance(p, new(a.X + t * dx, a.Y + t * dy));
    }
    private static double Distance(LayoutPoint a, LayoutPoint b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
