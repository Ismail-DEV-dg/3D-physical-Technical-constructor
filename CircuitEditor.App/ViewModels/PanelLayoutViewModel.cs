using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class PanelLayoutViewModel
{
    public DockPaneViewModel Components { get; } = new("Компоненты", 224);
    public DockPaneViewModel Inspector { get; } = new("Свойства и расчёт", 310);
    public DockPaneViewModel Analysis { get; } = new("Проверка / Расчёт DC", 140);
    [RelayCommand]
    private void Reset() { Components.Reset(); Inspector.Reset(); Analysis.Reset(); }
}
