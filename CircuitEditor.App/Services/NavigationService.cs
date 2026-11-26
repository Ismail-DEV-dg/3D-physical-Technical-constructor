using CircuitEditor.App.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CircuitEditor.App.Services;

public sealed class NavigationService : ObservableObject, INavigationService
{
    private WorkspaceViewModel current;

    public NavigationService(BreadboardViewModel mockup, SchematicViewModel schematic, ChipViewModel chip,
        CodeViewModel code)
    {
        Workspaces = Array.AsReadOnly<WorkspaceViewModel>([mockup, schematic, code, chip]);
        current = mockup;
    }

    public IReadOnlyList<WorkspaceViewModel> Workspaces { get; }
    public WorkspaceViewModel Current => current;

    public bool Navigate(WorkspaceKind kind)
    {
        var destination = Workspaces.FirstOrDefault(workspace => workspace.Kind == kind);
        if (destination is null)
            return false;

        SetProperty(ref current, destination, nameof(Current));
        return true;
    }
}
