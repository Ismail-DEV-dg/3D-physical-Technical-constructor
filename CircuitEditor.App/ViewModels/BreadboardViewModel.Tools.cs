using CircuitEditor.App.Interaction;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CircuitEditor.Core.Placement;

namespace CircuitEditor.App.ViewModels;

public sealed partial class BreadboardViewModel
{
    [ObservableProperty] private bool showWireLabels;
    [ObservableProperty] private bool showNetColors;
    [ObservableProperty] private bool isSimulationSettingsOpen;
    partial void OnShowWireLabelsChanged(bool value) => Revision++;
    partial void OnShowNetColorsChanged(bool value) => Revision++;

    private string? pendingComponent;
    private int? pendingRotation;
    internal bool IsPlacementArmed => pendingComponent is not null;

    [RelayCommand]
    private void CloseSimulationSettings() => IsSimulationSettingsOpen = false;

    [RelayCommand]
    private void ToggleWireLabels() => ShowWireLabels = !ShowWireLabels;

    internal void ArmPlacement(string definitionId)
    {
        if (!Project.CanEdit || Project.InGesture) return;
        SelectTool("Select");
        pendingComponent = definitionId;
        Project.StatusMessage = "Щёлкните по макету для размещения детали. R — поворот, Esc — отмена.";
    }

    [RelayCommand]
    private void ArmQuickPlacement(string? definitionId)
    {
        if (FindPaletteDefinition(definitionId) is { } definition) ArmPlacement(definition.Id);
    }

    [RelayCommand]
    private void SelectTool(string tool)
    {
        if (Project.InGesture) return;
        pendingComponent = null;
        pendingRotation = null;
        ClearPalettePreview();
        CancelNote();
        ClearRuler();
        ClearNetInspection();
        Project.Wiring.IsWiring = tool == "Wire";
    }

    private CanvasInput RouteTool(CanvasInput input)
    {
        if (input.Action == CanvasAction.Cancel) CloseSimulationSettings();
        if (input.Action == CanvasAction.Cancel || Project.Wiring.IsWiring)
        {
            pendingComponent = null;
            pendingRotation = null;
            ClearPalettePreview();
            CancelNote();
            ClearRuler();
            ClearNetInspection();
        }
        if (input.Action == CanvasAction.Drop) ClearNetInspection();
        if (input.Pan || !Project.CanEdit || pendingComponent is not { } id)
            return input;
        if (input.Action == CanvasAction.Move)
            return input with { Action = CanvasAction.DropPreview, DefinitionId = id, PlacementRotation = pendingRotation };
        if (input.Action != CanvasAction.Down) return input;

        var rotation = pendingRotation;
        pendingComponent = null;
        pendingRotation = null;
        return input with { Action = CanvasAction.Drop, DefinitionId = id, PlacementRotation = rotation };
    }

    private bool RotatePendingPlacement()
    {
        if (pendingComponent is not { } id) return false;
        pendingRotation = ((pendingRotation ?? PalettePreview?.Pose.Rotation ?? 0) + ComponentPlacement.RotationStep) %
            ComponentPlacement.FullRotation;
        if (PalettePreview is not null) PreviewPalette(new(CanvasAction.DropPreview, DefinitionId: id, PlacementRotation: pendingRotation));
        return true;
    }
}
