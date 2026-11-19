using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CircuitEditor.App.ViewModels;

public sealed partial class PanelLayoutViewModel : ObservableObject
{
    [ObservableProperty] private int toolTabIndex;
    public DockPaneViewModel Components { get; } = new("Компоненты", 224);
    public DockPaneViewModel Inspector { get; } = new("Свойства", 280);
    public DockPaneViewModel Analysis { get; } = new("Проблемы и расчёты", 280);
    [RelayCommand]
    private void Reset() { Components.Reset(); Inspector.Reset(); Analysis.Reset(); }
}
