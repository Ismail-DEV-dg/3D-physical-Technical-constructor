using System.Collections.ObjectModel;
using System.ComponentModel;
using CircuitEditor.App.Interaction;
using CircuitEditor.Core.Geometry;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class SchematicViewModel : WorkspaceViewModel, IDisposable
{
    public const double MinimumZoom = 0.25;
    public const double MaximumZoom = 3;
    private const double ZoomStep = 1.12;
    private const double Percent = 100;
    private const double WheelDeltaPerStep = 120;
    private const double DefaultOffset = 40;

    private readonly Dictionary<Guid, (double X, double Y)> moveOrigins = [];
    private double startX, startY, initialOffsetX, initialOffsetY;
    private bool moving, panning, additiveSelection;
    public SchematicViewModel(ProjectViewModel project)
        : base(WorkspaceKind.Schematic, "Схема", "02", "Холст схемы", "Производное представление проекта")
    {
        Project = project;
        Project.PropertyChanged += OnProjectChanged;
    }
    public ProjectViewModel Project { get; }
    public IReadOnlyList<ComponentDefinition> Library => Project.Library;
    public ObservableCollection<ComponentViewModel> Components => Project.Components;
    public ComponentViewModel? SelectedComponent => Project.SelectedComponent;
    public int SelectedCount => Project.SelectedCount;
    public int ComponentCount => Project.ComponentCount;
    public string StatusMessage { get => Project.StatusMessage; set => Project.StatusMessage = value; }
    public string PropertyError { get => Project.PropertyError; set => Project.PropertyError = value; }
    public double ZoomPercent => Zoom * Percent;
    public double SelectionLeft => Math.Min(startX, CursorX);
    public double SelectionTop => Math.Min(startY, CursorY);
    public double SelectionWidth => Math.Abs(CursorX - startX);
    public double SelectionHeight => Math.Abs(CursorY - startY);

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ZoomPercent))] private double zoom = 1;
    [ObservableProperty] private double offsetX = DefaultOffset;
    [ObservableProperty] private double offsetY = DefaultOffset;
    [ObservableProperty] private double cursorX;
    [ObservableProperty] private double cursorY;
    [ObservableProperty] private bool isSelecting;
    [ObservableProperty] private int revision;



    public double ViewportWidth { get; private set; }=800;
    public double ViewportHeight { get; private set; }=600;
    [RelayCommand]
    private void Input(CanvasInput input)
    {
        if(input.Action==CanvasAction.Resize){ViewportWidth=input.X;ViewportHeight=input.Y;return;}
        if (input.Action is CanvasAction.Rotate) { RotateSelected(); return; }
        if (input.Action is CanvasAction.Delete) { DeleteSelected(); return; }
        if (input.Action is CanvasAction.Cancel) { CancelGesture(); return; }
        if (input.Action is CanvasAction.SelectAll)
        {
            foreach (var item in Components) item.IsSelected = true;
            Refresh(); return;
        }
        if (input.Action is CanvasAction.Down or CanvasAction.Drop && !input.Pan) Project.BeginGesture("Схема: размещение");
        CursorX = (input.X - OffsetX) / Zoom;
        CursorY = (input.Y - OffsetY) / Zoom;
        switch (input.Action)
        {
            case CanvasAction.Drop:
                var definition = Library.FirstOrDefault(item => item.Id == input.DefinitionId);
                if (definition is not null)
                {
                    var added = AddComponent(definition, CursorX, CursorY);
                    added.SchematicPlacement.MoveTo(DiagramGeometry.Snap(CursorX - added.Layout.Width / 2),
                        DiagramGeometry.Snap(CursorY - added.Layout.Height / 2));
                }
                break;
            case CanvasAction.Down: BeginGesture(input); break;
            case CanvasAction.Move: MoveGesture(input); break;
            case CanvasAction.Up: EndGesture(); break;
            case CanvasAction.Wheel: ZoomAt(input); break;
        }
        if (input.Action is CanvasAction.Up or CanvasAction.Drop) Project.EndGesture();
        Revision++;
    }

    public ComponentViewModel AddComponent(ComponentDefinition definition, double x, double y)
    {
        var own = Project.BeginGesture("Добавление компонента");
        var added = Project.Add(definition) ?? throw new InvalidOperationException(Project.StatusMessage);
        added.SchematicPlacement.MoveTo(DiagramGeometry.Snap(x), DiagramGeometry.Snap(y));
        if (own) Project.EndGesture();
        Refresh();
        return added;
    }

    private void BeginGesture(CanvasInput input)
    {
        startX = CursorX; startY = CursorY;
        initialOffsetX = OffsetX; initialOffsetY = OffsetY;
        moveOrigins.Clear();
        if (input.Pan)
        {
            panning = true; return;
        }
        var hit = Components.LastOrDefault(item => DiagramGeometry.Contains(item.SchematicPlacement, CursorX, CursorY,
            item.Layout.Width, item.Layout.Height));
        if (hit is null)
        {
            additiveSelection = input.Control;
            if (!input.Control) foreach (var item in Components) item.IsSelected = false;
            IsSelecting = true;
        }
        else
        {
            if (input.Control) hit.IsSelected = !hit.IsSelected;
            else if (!hit.IsSelected)
            {
                foreach (var item in Components) item.IsSelected = false;
                hit.IsSelected = true;
            }
            moving = hit.IsSelected;
            foreach (var item in Components.Where(item => item.IsSelected))
                moveOrigins[item.Model.Id] = (item.SchematicPlacement.X, item.SchematicPlacement.Y);
        }
        Refresh();
    }

    private void MoveGesture(CanvasInput input)
    {
        if (panning)
        {
            OffsetX = input.X - startX * Zoom;
            OffsetY = input.Y - startY * Zoom;
        }
        else if (moving)
        {
            var dx = DiagramGeometry.Snap(CursorX - startX);
            var dy = DiagramGeometry.Snap(CursorY - startY);
            foreach (var item in Components.Where(item => moveOrigins.ContainsKey(item.Model.Id)))
            {
                var origin = moveOrigins[item.Model.Id];
                item.SchematicPlacement.MoveTo(origin.X + dx, origin.Y + dy);
            }
        }
    }

    private void EndGesture()
    {
        if (IsSelecting)
        {
            foreach (var item in Components)
                item.IsSelected = (additiveSelection && item.IsSelected) ||
                    DiagramGeometry.Intersects(item.SchematicPlacement, SelectionLeft, SelectionTop,
                        SelectionLeft + SelectionWidth, SelectionTop + SelectionHeight, item.Layout.Width, item.Layout.Height);
        }
        moving = panning = IsSelecting = false;
        moveOrigins.Clear();
        Refresh();
    }

    private void ZoomAt(CanvasInput input)
    {
        if (moving || panning || IsSelecting) return;
        var next = Math.Clamp(Zoom * Math.Pow(ZoomStep, input.Delta / WheelDeltaPerStep), MinimumZoom, MaximumZoom);
        OffsetX = input.X - CursorX * next;
        OffsetY = input.Y - CursorY * next;
        Zoom = next;
        OnPropertyChanged(nameof(ZoomPercent));
    }

    [RelayCommand]
    private void RotateSelected()
    {
        Project.BeginGesture("Поворот символов");
        foreach (var item in Components.Where(item => item.IsSelected)) item.SchematicPlacement.Rotate();
        Project.EndGesture();
        Refresh();
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        Project.RemoveSelected();
        Refresh();
    }

    [RelayCommand]
    private void ResetView()
    {
        CancelGesture();
        Zoom = 1; OffsetX = OffsetY = DefaultOffset;
        OnPropertyChanged(nameof(ZoomPercent));
        Refresh();
    }

    [RelayCommand]
    private void ShowAllComponents()
    {
        Project.BeginGesture("Галерея компонентов");
        const int columns = 5;
        var spacing = 3 * DiagramGeometry.GridSize;
        var originX = Components.Count == 0 ? 0 : DiagramGeometry.Snap(Components.Max(item =>
            item.SchematicPlacement.X + Math.Max(item.Layout.Width, item.Layout.Height)) + spacing);
        var gallery = Library.Select(definition => AddComponent(definition, 0, 0)).ToArray();
        var cellWidth = gallery.Select(item => item.Layout.Width).DefaultIfEmpty().Max() + spacing;
        var rowY = 0d;
        foreach (var row in gallery.Chunk(columns))
        {
            for (var column = 0; column < row.Length; column++)
            {
                row[column].SchematicPlacement.MoveTo(originX + column * cellWidth, rowY);
                row[column].IsSelected = false;
            }
            rowY += row.Max(item => item.Layout.Height) + spacing;
        }
        Zoom = MinimumZoom;
        OffsetX = DefaultOffset - originX * Zoom;
        OffsetY = DefaultOffset;
        OnPropertyChanged(nameof(ZoomPercent));
        Project.EndGesture();
        StatusMessage = $"Галерея: {gallery.Length} компонентов · колесо — масштаб, ПКМ — панорама.";
        Refresh();
    }

    [RelayCommand]
    private void ApplyProperties() => Project.ApplyPropertiesCommand.Execute(null);

    private void CancelGesture()
    {
        foreach (var item in Components.Where(item => moveOrigins.ContainsKey(item.Model.Id)))
        {
            var origin = moveOrigins[item.Model.Id];
            item.SchematicPlacement.MoveTo(origin.X, origin.Y);
        }
        if (panning) { OffsetX = initialOffsetX; OffsetY = initialOffsetY; }
        moving = panning = IsSelecting = false;
        moveOrigins.Clear();
        Project.CancelGesture();
        Refresh();
    }

    private void Refresh() => Project.Refresh();
    private void OnProjectChanged(object? sender, PropertyChangedEventArgs args)
    {
        Revision++;
        if (args.PropertyName is nameof(Project.SelectedComponent) or nameof(Project.SelectedCount)
            or nameof(Project.ComponentCount) or nameof(Project.StatusMessage) or nameof(Project.PropertyError))
            OnPropertyChanged(args.PropertyName);
    }
    public void Dispose() => Project.PropertyChanged -= OnProjectChanged;
}
