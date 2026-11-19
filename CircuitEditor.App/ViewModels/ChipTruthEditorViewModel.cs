using System.Text;
using CircuitEditor.App.Services;
using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class ChipTruthEditorViewModel(ChipViewModel editor, IDialogService? dialogs,
    IChipCsvRepository? csv, IExportRepository? exports) : ObservableObject
{
    private const string CsvFilter = "Таблица истинности (*.csv)|*.csv";
    private ChipTruthTable table = new([]);
    [ObservableProperty] private int selectedSection = -1;
    [ObservableProperty] private string inputPins = "";
    [ObservableProperty] private string outputPins = "";

    public IReadOnlyList<string> Sections => table.Sections.Select((section, index) =>
        $"Секция {index + 1}: {string.Join(", ", section.Inputs)} → {string.Join(", ", section.Outputs)}").ToArray();
    public IReadOnlyList<ChipTruthRowEdit> Rows { get; private set; } = [];

    internal void Load(ChipTruthTable value)
    {
        table = value;
        var next = table.Sections.Count == 0 ? -1 : Math.Clamp(SelectedSection, 0, table.Sections.Count - 1);
        OnPropertyChanged(nameof(Sections));
        SelectedSection = next;
        SetSection();
    }

    partial void OnSelectedSectionChanged(int value) => SetSection();

    [RelayCommand]
    private void AddSection()
    {
        SelectedSection = -1;
        InputPins = string.Join(", ", editor.Definition.Pins.Where(pin => pin.Reads)
            .Select(pin => pin.Number).Order().Take(ChipTruthSection.MaximumInputs));
        OutputPins = string.Join(", ", editor.Definition.Pins.Where(pin => pin.Drives &&
            !table.Sections.Any(section => section.Outputs.Contains(pin.Number)))
            .Select(pin => pin.Number).Order().Take(ChipTruthSection.MaximumOutputs));
    }

    [RelayCommand]
    private void Generate()
    {
        editor.Edit(() =>
        {
            var inputs = ChipEditorText.Numbers(InputPins);
            var outputs = ChipEditorText.Numbers(OutputPins);
            if (inputs.Length > ChipTruthSection.MaximumInputs || outputs.Length is < 1 or > ChipTruthSection.MaximumOutputs)
                throw new FormatException("Укажите до 6 входов и от 1 до 8 выходов.");
            if (inputs.Any(number => !editor.Definition.Pins.Any(pin => pin.Number == number && pin.Reads)) ||
                outputs.Any(number => !editor.Definition.Pins.Any(pin => pin.Number == number && pin.Drives)))
                throw new FormatException("Указанные номера должны соответствовать входам и выходам микросхемы.");
            Replace(new(inputs, outputs, ChipTruthLogic.Patterns(inputs.Length)
                .Select(pattern => (IReadOnlyList<string>)new[] { pattern, new string('X', outputs.Length) }).ToArray()));
        });
    }

    [RelayCommand]
    private void ApplyCells()
    {
        if (SelectedSection < 0 || SelectedSection >= table.Sections.Count) return;
        var section = table.Sections[SelectedSection];
        editor.Edit(() =>
        {
            var rows = Rows.Select(row => new[] { row.Inputs, row.Outputs.Trim().ToUpperInvariant() }).ToArray();
            if (rows.Any(row => row[1].Length != section.Outputs.Count || row[1].Any(value => value is not ('0' or '1' or 'X'))))
                throw new FormatException("В каждой строке введите 0, 1 или X для каждого выхода.");
            Replace(section with { Rows = rows });
        });
    }

    [RelayCommand]
    private void RemoveSection()
    {
        if (SelectedSection >= 0)
            editor.ChangeBehavior(new ChipTruthTable(table.Sections.Where((_, index) => index != SelectedSection).ToArray()));
    }

    [RelayCommand]
    private void ImportCsv()
    {
        if (csv is null || dialogs?.OpenFile(CsvFilter) is not { } path) return;
        var result = csv.Load(path);
        if (!result.Success) { dialogs.ShowError(result.Error!); return; }
        editor.Edit(() =>
        {
            var imported = ChipTruthCsv.Parse(result.Value!);
            var errors = editor.Validate(editor.Definition with { Behavior = imported });
            if (errors.Count != 0) throw new FormatException(string.Join(Environment.NewLine, errors));
            editor.ChangeBehavior(imported);
        });
    }

    [RelayCommand]
    private void ExportCsv()
    {
        if (!editor.CheckReady() || exports is null || dialogs?.SaveFile(CsvFilter, ".csv", editor.Definition.Name) is not { } path) return;
        var encoding = new UTF8Encoding(true);
        if (exports.Write(path, [.. encoding.GetPreamble(), .. encoding.GetBytes(ChipTruthCsv.Format(table))]) is { } error)
            dialogs.ShowError(error);
    }

    private void Replace(ChipTruthSection newSection)
    {
        var sections = table.Sections.ToList();
        var index = SelectedSection;
        if (index < 0) { index = sections.Count; sections.Add(newSection); }
        else sections[index] = newSection;
        editor.ChangeBehavior(new ChipTruthTable(sections.ToArray()));
        SelectedSection = index;
    }

    private void SetSection()
    {
        var section = SelectedSection >= 0 && SelectedSection < table.Sections.Count ? table.Sections[SelectedSection] : null;
        InputPins = section is null ? "" : string.Join(", ", section.Inputs);
        OutputPins = section is null ? "" : string.Join(", ", section.Outputs);
        Rows = section?.Rows.Select(row => new ChipTruthRowEdit(row)).ToArray() ?? [];
        OnPropertyChanged(nameof(Rows));
    }
}
