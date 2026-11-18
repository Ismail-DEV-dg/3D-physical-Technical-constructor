using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class DockPaneViewModel(string title, double initialSize) : ObservableObject
{
    public string Title { get; } = title;
    private readonly double defaultSize = initialSize;
    [ObservableProperty] private double size = initialSize;
    [ObservableProperty] private bool isVisible = true;
    [ObservableProperty] private bool isPinned = true;
    [ObservableProperty] private bool isOpen = true;
    public bool IsExpanded => IsVisible && (IsPinned || IsOpen);
    partial void OnIsVisibleChanged(bool value) => OnPropertyChanged(nameof(IsExpanded));
    partial void OnIsPinnedChanged(bool value) => OnPropertyChanged(nameof(IsExpanded));
    partial void OnIsOpenChanged(bool value) => OnPropertyChanged(nameof(IsExpanded));

    [RelayCommand]
    private void TogglePin()
    {
        IsOpen = !IsPinned;
        IsPinned = !IsPinned;
    }
    [RelayCommand] private void ToggleOpen() => IsOpen = !IsOpen;
    [RelayCommand] private void Hide() => IsVisible = false;
    [RelayCommand] private void Show() { IsVisible = true; IsOpen = true; }
    public void CollapseIfUnpinned() { if (!IsPinned) IsOpen = false; }
    public void Reset() { Size = defaultSize; IsPinned = true; IsOpen = true; IsVisible = true; }
}
