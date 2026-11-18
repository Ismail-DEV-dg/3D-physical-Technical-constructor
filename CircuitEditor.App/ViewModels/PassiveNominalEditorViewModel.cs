using System.Collections.ObjectModel;
using CircuitEditor.Core.Quantities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class PassiveNominalEditorViewModel : ObservableObject
{
    private readonly Action<string> apply;
    private readonly SortedSet<decimal> custom = [];
    private bool rebuilding;
    public PassiveNominalEditorViewModel(PassiveQuantity quantity, string initial, Action<string> apply)
    {
        Quantity = quantity; this.apply = apply;
        Rebuild(PassiveNominals.TryParse(quantity, initial, out var value) ? value : null);
    }
    public PassiveQuantity Quantity { get; }
    public string Caption => Quantity == PassiveQuantity.Capacitance ? "ЁМКОСТЬ · E12" : "ИНДУКТИВНОСТЬ · E12";
    public string Hint => Quantity == PassiveQuantity.Capacitance ? "1 пФ … 1000 мкФ; например 4,7 нФ" : "1 мкГн … 100 мГн; например 2,2 мГн";
    public ObservableCollection<PassiveNominalOption> Options { get; } = [];
    [ObservableProperty] private PassiveNominalOption? selectedOption;
    [ObservableProperty] private bool isCustomEntry;
    [ObservableProperty] private string customText = "";
    [ObservableProperty] private string error = "";
    partial void OnSelectedOptionChanged(PassiveNominalOption? value)
    {
        if (rebuilding || value is null) return;
        IsCustomEntry = value.Value is null; Error = "";
        if (value.Value is { } number) apply(PassiveNominals.Format(Quantity, number));
    }
    [RelayCommand]
    private void ApplyCustom()
    {
        if (!PassiveNominals.TryParse(Quantity, CustomText, out var value))
        { Error = Hint; return; }
        custom.Add(value); Error = ""; IsCustomEntry = false;
        Rebuild(value); apply(PassiveNominals.Format(Quantity, value));
    }
    private void Rebuild(decimal? selected)
    {
        rebuilding = true;
        var values = new SortedSet<decimal>(PassiveNominals.GetValues(Quantity));
        if (selected is { } value) custom.Add(value);
        values.UnionWith(custom); Options.Clear();
        foreach (var number in values) Options.Add(new(Quantity, number));
        Options.Add(new(Quantity, null));
        SelectedOption = Options.First(option => option.Value == selected);
        rebuilding = false;
    }
}
