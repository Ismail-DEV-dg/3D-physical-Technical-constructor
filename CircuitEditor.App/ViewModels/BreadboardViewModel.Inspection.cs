using CircuitEditor.App.Interaction;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Netlist;
using CircuitEditor.Core.Wiring;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class BreadboardViewModel
{
    [ObservableProperty] private bool showGrid = true;
    [ObservableProperty] private bool isNetInspectionActive;
    private WireEndpoint? inspectedContact;

    public bool CanFocusSelection => Project.SelectedCount > 0;

    [RelayCommand]
    private void FocusSelection()
    {
        if (!CanFocusSelection || Project.InGesture) return;
        WorkspaceViewport.FitSelection(this);
    }

    partial void OnIsNetInspectionActiveChanged(bool value)
    {
        if (value)
        {
            if (Project.InGesture) { IsNetInspectionActive = false; return; }
            CancelNote();
            ClearRuler();
            Project.Wiring.IsWiring = false;
            pendingComponent = null;
            Project.StatusMessage = "Щёлкните по выводу или отверстию, чтобы подсветить электрический узел. Esc — выйти.";
        }
        else
        {
            inspectedContact = null;
            Revision++;
        }
    }

    internal Net? InspectedNet => inspectedContact is { } contact
        ? Project.Connectivity.Netlist.Find(contact)
        : null;

    private bool RouteNetInspection(CanvasInput input)
    {
        if (!IsNetInspectionActive || input.Pan || input.Action != CanvasAction.Down)
            return false;

        inspectedContact = WireGeometry.Nearest(Project.Model, new LayoutPoint(CursorX, CursorY));
        Project.StatusMessage = InspectedNet is { } net
            ? $"Узел {net.Name}. Выводов: {net.Pins.Count}."
            : "Контакт не найден. Щёлкните по выводу или отверстию.";
        Revision++;
        return true;
    }

    private void ClearNetInspection() => IsNetInspectionActive = false;
}
