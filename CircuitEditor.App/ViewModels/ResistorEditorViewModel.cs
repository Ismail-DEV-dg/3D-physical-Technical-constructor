using System.Collections.ObjectModel;
using CircuitEditor.Core.Resistance;
using CircuitEditor.Core.Physical;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class ResistorEditorViewModel : ObservableObject
{
    private readonly Action<decimal> apply;
    private readonly SortedSet<decimal> customValues = [];
    private decimal? current;
    private bool rebuilding;

    public ResistorEditorViewModel(string initialValue, Action<decimal> apply)
    {
        this.apply = apply;
        if (ResistanceFormatter.TryParse(initialValue, out var value)) current = value;
        UpdateMarkingNote();
        Rebuild();
    }

    public IReadOnlyList<ResistorSeriesKind> Series { get; } = Enum.GetValues<ResistorSeriesKind>();
    public ObservableCollection<ResistanceOption> Options { get; } = [];
    [ObservableProperty] private ResistorSeriesKind selectedSeries = ResistorSeriesKind.E12;
    [ObservableProperty] private ResistanceOption? selectedOption;
    [ObservableProperty] private bool isCustomEntry;
    [ObservableProperty] private string customText = "";
    [ObservableProperty] private string error = "";
    [ObservableProperty] private string markingNote = "";

    partial void OnSelectedSeriesChanged(ResistorSeriesKind value) => Rebuild();
    partial void OnSelectedOptionChanged(ResistanceOption? value)
    {
        if (rebuilding || value is null) return;
        IsCustomEntry = value.Ohms is null;
        Error = "";
        if (value.Ohms is { } ohms) { current = ohms; UpdateMarkingNote(); apply(ohms); }
    }

    [RelayCommand]
    private void ApplyCustom()
    {
        if (!ResistanceFormatter.TryParse(CustomText, out var value))
        { Error = "Введите число и единицу: 1 Ω … 10 MΩ (например, 2,35 kΩ)."; return; }
        customValues.Add(value); current = value;
        Error = ""; IsCustomEntry = false;
        Rebuild(); UpdateMarkingNote(); apply(value);
    }

    private void UpdateMarkingNote() => MarkingNote = current is { } ohms &&
        ResistorMarking.GetBands(ResistanceFormatter.Format(ohms)).Count == 0
        ? "Номинал сохранён точно. Для него нет точной маркировки из 4–5 полос; на корпусе показан знак «?»." : "";

    private void Rebuild()
    {
        rebuilding = true;
        var values = new SortedSet<decimal>(ResistorSeries.GetValues(SelectedSeries));
        if (current is { } value && !values.Contains(value)) customValues.Add(value);
        values.UnionWith(customValues);
        Options.Clear();
        foreach (var ohms in values) Options.Add(new ResistanceOption(ohms));
        Options.Add(new ResistanceOption(null));
        SelectedOption = Options.FirstOrDefault(option => option.Ohms == current) ?? Options[^1];
        rebuilding = false;
    }
}
