using CommunityToolkit.Mvvm.ComponentModel;

namespace CircuitEditor.App.ViewModels;

public sealed partial class SimulationNumberViewModel(string name, double value) : ObservableObject
{
    public string Name { get; } = name;
    [ObservableProperty] private double value = value;
}
