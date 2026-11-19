using CommunityToolkit.Mvvm.ComponentModel;

namespace CircuitEditor.App.ViewModels;

public sealed partial class ScenarioPinViewModel(string name, IReadOnlyList<SimulationPinOption> options) : ObservableObject
{
    public string Name { get; } = name;
    public IReadOnlyList<SimulationPinOption> Options { get; } = options;
    [ObservableProperty] private SimulationPinOption? selected;
}
