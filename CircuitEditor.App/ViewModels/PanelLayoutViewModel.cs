using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CircuitEditor.App.ViewModels;

public sealed partial class PanelLayoutViewModel : ObservableObject
{
    [ObservableProperty] private int toolTabIndex;
    public DockPaneViewModel Components { get; } = new("Компоненты", 224);
    public DockPaneViewModel Inspector { get; } = new("Свойства и расчёт", 310);
    public DockPaneViewModel Analysis { get; } = new("Проверка / Код / Вывод", 140);
    [RelayCommand]
    private void Reset() { Components.Reset(); Inspector.Reset(); Analysis.Reset(); }
}
