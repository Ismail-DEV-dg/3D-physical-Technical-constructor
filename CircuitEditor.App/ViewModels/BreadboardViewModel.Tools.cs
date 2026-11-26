using CircuitEditor.App.Interaction;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class BreadboardViewModel
{
    [ObservableProperty] private bool showWireLabels;
    [ObservableProperty] private bool showNetColors;
    [ObservableProperty] private bool isSimulationSettingsOpen;
    partial void OnShowWireLabelsChanged(bool value) => Revision++;
    partial void OnShowNetColorsChanged(bool value) => Revision++;

    private string? pendingComponent;

    [RelayCommand]
    private void CloseSimulationSettings() => IsSimulationSettingsOpen = false;

    [RelayCommand]
    private void ToggleWireLabels() => ShowWireLabels = !ShowWireLabels;

    internal void ArmPlacement(string definitionId)
    {
        if (!Project.CanEdit || Project.InGesture) return;
        SelectTool("Select");
        pendingComponent = definitionId;
        Project.StatusMessage = "Щёлкните по макету для размещения детали. Esc — отмена.";
    }

    [RelayCommand]
    private void SelectTool(string tool)
    {
        if (Project.InGesture) return;
        pendingComponent = null;
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
            CancelNote();
            ClearRuler();
            ClearNetInspection();
        }
        if (input.Action == CanvasAction.Drop) ClearNetInspection();
        if (input.Action != CanvasAction.Down || input.Pan || !Project.CanEdit || pendingComponent is not { } id)
            return input;

        pendingComponent = null;
        return input with { Action = CanvasAction.Drop, DefinitionId = id };
    }
}
