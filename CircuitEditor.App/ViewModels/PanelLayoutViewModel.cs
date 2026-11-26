using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CircuitEditor.App.ViewModels;

public sealed partial class PanelLayoutViewModel : ObservableObject
{
    [ObservableProperty] private int toolTabIndex;
    public DockPaneViewModel Components { get; } = new("Библиотека", 280);
    public DockPaneViewModel Inspector { get; } = new("Свойства", 280);
    public DockPaneViewModel Analysis { get; } = new("Проблемы и расчёты", 280);
    public PanelLayoutViewModel()
    {
        Analysis.IsVisible = false;
        Inspector.IsOpen = false;
    }

    [RelayCommand]
    private void ToggleAnalysis()
    {
        if (Analysis.IsExpanded) Analysis.IsVisible = false;
        else
        {
            Analysis.IsVisible = true;
            Analysis.IsOpen = true;
        }
    }

    [RelayCommand]
    private void Reset()
    {
        Components.Reset();
        Inspector.Reset();
        Analysis.Reset();
        Analysis.IsVisible = false;
    }
}
