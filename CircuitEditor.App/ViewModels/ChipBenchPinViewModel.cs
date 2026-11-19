using CircuitEditor.Core.Chips;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed class ChipBenchPinViewModel(ChipPin pin, ChipLevel level, IRelayCommand? toggle)
{
    public int Number => pin.Number;
    public string Label => $"{pin.Number} · {pin.Name}";
    public string Value => ChipTruthLogic.Symbol(level).ToString();
    public IRelayCommand? Toggle { get; } = toggle;
}
