using System.ComponentModel;
using CircuitEditor.App.ViewModels;

namespace CircuitEditor.App.Services;

public interface INavigationService : INotifyPropertyChanged
{
    IReadOnlyList<WorkspaceViewModel> Workspaces { get; }
    WorkspaceViewModel Current { get; }
    bool Navigate(WorkspaceKind kind);
}

