using CommunityToolkit.Mvvm.ComponentModel;

namespace CircuitEditor.App.ViewModels;

public sealed partial class SimulationNumberViewModel(string name, double value) : ObservableObject
{
    public string Name { get; } = name;
    public string Label => Name switch
    {
        "periodMs" => "Период, мс",
        "duty" => "Доля времени включения, 0–1",
        "activeLow" => "Кнопка: 1 — активный LOW, 0 — HIGH",
        "minimumAngle" => "Минимальный угол, °",
        "maximumAngle" => "Максимальный угол, °",
        _ => Name
    };
    [ObservableProperty] private double value = value;
}
