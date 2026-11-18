using System.ComponentModel;
using CircuitEditor.App.Interaction;
using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class BreadboardViewModel : WorkspaceViewModel, IDisposable
{
    private readonly IComponentInsertionService insertion;
    private ComponentViewModel? dragging;
    private ComponentPlacement? draft;
    private double grabX, grabY, startX, startY, oldOffsetX, oldOffsetY;
    private bool panning;
    private Guid? selectionId;
    public BreadboardViewModel(ProjectViewModel project, IComponentInsertionService insertion)
        : base(WorkspaceKind.Mockup, "Макет", "01", "Физический макет", "Breadboard и компоненты")
    {
        Project = project; this.insertion = insertion;
        Project.PropertyChanged += OnProjectChanged;
    }
    public ProjectViewModel Project { get; }
    public BreadboardInstance? Board => Project.Model.Breadboards.FirstOrDefault();
    public InsertionResult? Preview { get; private set; }
    public bool ShowAvailableHoles => dragging?.Footprint.Mounting == "breadboard"
        || Project.SelectedComponent?.Footprint.Mounting == "breadboard";
    public double ZoomPercent => Zoom * 100;
    public ComponentPlacement PlacementOf(ComponentViewModel item) =>
        item == dragging && draft is not null ? draft : item.MockupPlacement;
    public IReadOnlyList<int> SpanOptions => Project.SelectedComponent?.Footprint is { IsFlexible: true } footprint
        ? Enumerable.Range(footprint.MinSpan!.Value, footprint.MaxSpan!.Value - footprint.MinSpan.Value + 1).ToArray() : [];
    public bool CanAdjustSpan => Project.SelectedComponent?.Footprint.IsFlexible == true;
    public int SelectedSpan
    {
        get => Project.SelectedComponent is { } item
            ? (item.MockupPlacement.SpanHoles == 0 ? item.Footprint.DefaultSpan : item.MockupPlacement.SpanHoles) : 0;
        set
        {
            if (!CanAdjustSpan || !SpanOptions.Contains(value) || value == SelectedSpan) return;
            var item = Project.SelectedComponent!;
            var current = item.MockupPlacement;
            TryPlace(item, new(current.X, current.Y, current.Rotation, value));
            OnPropertyChanged();
        }
    }
    [ObservableProperty] private double zoom = 1;
    [ObservableProperty] private double offsetX = 16;
    [ObservableProperty] private double offsetY = 12;
    [ObservableProperty] private double cursorX;
    [ObservableProperty] private double cursorY;
    [ObservableProperty] private int revision;

    [RelayCommand]
    private void Input(CanvasInput input)
    {
        if (input.Action == CanvasAction.Cancel) { Cancel(); return; }
        if (input.Action == CanvasAction.Rotate) { RotateSelected(); return; }
        if (input.Action == CanvasAction.Delete) { Cancel(); Project.RemoveSelected(); return; }
        CursorX = PhysicalSettings.ToMillimeters((input.X - OffsetX) / Zoom);
        CursorY = PhysicalSettings.ToMillimeters((input.Y - OffsetY) / Zoom);
        switch (input.Action)
        {
            case CanvasAction.Down:
                if (input.Pan)
                {
                    panning = true; startX = input.X; startY = input.Y; oldOffsetX = OffsetX; oldOffsetY = OffsetY;
                    break;
                }
                dragging = Project.Components.LastOrDefault(item => Hit(item, CursorX, CursorY));
                Project.Select(dragging, input.Control);
                if (dragging is not null && dragging.IsSelected)
                {
                    draft = dragging.MockupPlacement.Copy();
                    grabX = CursorX - draft.X; grabY = CursorY - draft.Y;
                }
                else dragging = null;
                break;
            case CanvasAction.Move:
                if (panning) { OffsetX = oldOffsetX + input.X - startX; OffsetY = oldOffsetY + input.Y - startY; }
                else if (dragging is not null && draft is not null)
                    UpdateDraft(new(CursorX - grabX, CursorY - grabY, draft.Rotation, draft.SpanHoles));
                break;
            case CanvasAction.Up:
                if (dragging is not null && draft is not null) TryPlace(dragging, draft);
                dragging = null; draft = null; Preview = null; panning = false;
                break;
            case CanvasAction.Drop:
                var definition = Project.Library.FirstOrDefault(item => item.Id == input.DefinitionId);
                if (definition is null) break;
                var added = Project.Add(definition);
                if (added is null) break;
                var origin = added.Footprint.Mounting == "breadboard"
                    ? new ComponentPlacement(CursorX, CursorY)
                    : new ComponentPlacement(CursorX - added.Footprint.Body.Width / 2, CursorY - added.Footprint.Body.Height / 2);
                if (!TryPlace(added, origin))
                {
                    var error = Project.StatusMessage;
                    Project.RemoveSelected();
                    Project.StatusMessage = error;
                }
                break;
            case CanvasAction.Wheel:
                if (dragging is not null || panning) break;
                var next = Math.Clamp(Zoom * Math.Pow(1.12, input.Delta / 120d), 0.25, 3);
                OffsetX = input.X - PhysicalSettings.ToPixels(CursorX) * next;
                OffsetY = input.Y - PhysicalSettings.ToPixels(CursorY) * next;
                Zoom = next; OnPropertyChanged(nameof(ZoomPercent));
                break;
            case CanvasAction.SelectAll:
                foreach (var item in Project.Components) item.IsSelected = true;
                break;
        }
        Revision++;
    }

    public bool Hit(ComponentViewModel item, double x, double y)
    {
        var pose = PlacementOf(item);
        var bounds = ContactGeometry.Bounds(item.Footprint, pose, Board?.Definition.HolePitch ?? PhysicalSettings.FreeGridMm);
        const double pickPaddingMm = 1.2;
        return x >= pose.X + bounds.X - pickPaddingMm && x <= pose.X + bounds.Right + pickPaddingMm
            && y >= pose.Y + bounds.Y - pickPaddingMm && y <= pose.Y + bounds.Bottom + pickPaddingMm;
    }

    private bool OnBoard(ComponentViewModel item, ComponentPlacement pose)
    {
        if (Board is not { } board || item.Footprint.Contacts.Count == 0) return false;
        var point = ContactGeometry.World(item.Footprint, item.Footprint.Contacts[0], pose, board.Definition.HolePitch);
        return point.X >= board.X && point.X <= board.X + board.Definition.Width
            && point.Y >= board.Y && point.Y <= board.Y + board.Definition.Height;
    }

    private ComponentPlacement CaptureHole(ComponentViewModel item, ComponentPlacement pose)
    {
        if (Board is not { } board || item.Footprint.Contacts.Count == 0) return pose;
        var first = ContactGeometry.World(item.Footprint, item.Footprint.Contacts[0], pose, board.Definition.HolePitch);
        var hole = board.Definition.Holes.MinBy(hole =>
            Math.Pow(hole.Position.X + board.X - first.X, 2) + Math.Pow(hole.Position.Y + board.Y - first.Y, 2))!;
        var dx = hole.Position.X + board.X - first.X; var dy = hole.Position.Y + board.Y - first.Y;
        return Math.Sqrt(dx * dx + dy * dy) <= PhysicalSettings.HoleCaptureRadiusMm
            ? new ComponentPlacement(pose.X + dx, pose.Y + dy, pose.Rotation, pose.SpanHoles) : pose;
    }

    private void UpdateDraft(ComponentPlacement proposed)
    {
        if (dragging is null) return;
        draft = proposed; Preview = null;
        if (dragging.Footprint.Mounting == "breadboard" && OnBoard(dragging, proposed) && Board is { } board)
        {
            draft = CaptureHole(dragging, proposed);
            Preview = insertion.Preview(Project.Model, dragging.Model.Id, dragging.Footprint, board.Id, draft);
            if (Preview.Success) draft = Preview.Placement;
        }
    }

    public bool TryPlace(ComponentViewModel item, ComponentPlacement proposed)
    {
        if (item.Footprint.IsFlexible)
        {
            var span = proposed.SpanHoles == 0 ? item.Footprint.DefaultSpan : proposed.SpanHoles;
            if (span < item.Footprint.MinSpan || span > item.Footprint.MaxSpan)
            { Project.StatusMessage = "Недопустимый шаг ножек."; return false; }
        }
        if (item.Footprint.Mounting == "breadboard" && OnBoard(item, proposed) && Board is { } board)
        {
            var result = insertion.Insert(Project.Model, item.Model.Id, item.Footprint, board.Id, CaptureHole(item, proposed));
            if (!result.Success) { Project.StatusMessage = result.Error!; return false; }
            Project.StatusMessage = $"{item.Reference}: все контакты вставлены.";
        }
        else
        {
            Project.Model.Detach(item.Model.Id);
            Project.Model.MockupLayout.Set(item.Model.Id, new ComponentPlacement(
                PhysicalSettings.SnapFree(proposed.X), PhysicalSettings.SnapFree(proposed.Y), proposed.Rotation, proposed.SpanHoles));
            Project.StatusMessage = $"{item.Reference}: свободное размещение рядом с платой.";
        }
        Project.Refresh();
        return true;
    }

    [RelayCommand]
    private void RotateSelected()
    {
        Cancel();
        if (Project.SelectedComponent is not { } item) return;
        var pose = item.MockupPlacement.Copy(); pose.Rotate();
        TryPlace(item, pose);
    }
    [RelayCommand]
    private void DeleteSelected() { Cancel(); Project.RemoveSelected(); }
    [RelayCommand]
    private void ResetView() { Cancel(); Zoom = 1; OffsetX = 16; OffsetY = 12; OnPropertyChanged(nameof(ZoomPercent)); Revision++; }
    private void Cancel()
    {
        if (panning) { OffsetX = oldOffsetX; OffsetY = oldOffsetY; }
        dragging = null; draft = null; Preview = null; panning = false; Revision++;
    }
    private void OnProjectChanged(object? sender, PropertyChangedEventArgs args)
    {
        Revision++;
        if (selectionId == Project.SelectedComponent?.Model.Id) return;
        selectionId = Project.SelectedComponent?.Model.Id;
        OnPropertyChanged(nameof(SpanOptions)); OnPropertyChanged(nameof(SelectedSpan)); OnPropertyChanged(nameof(CanAdjustSpan));
    }
    public void Dispose() => Project.PropertyChanged -= OnProjectChanged;
}
