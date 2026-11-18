using CommunityToolkit.Mvvm.ComponentModel;

namespace CircuitEditor.App.ViewModels;

public abstract class WorkspaceViewModel(WorkspaceKind kind, string title, string index,
    string heading, string description) : ObservableObject
{
    public WorkspaceKind Kind { get; } = kind;
    public string Title { get; } = title;
    public string Index { get; } = index;
    public string Heading { get; } = heading;
    public string Description { get; } = description;
}
