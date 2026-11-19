using CircuitEditor.App.Services;
using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Simulation.Instruments;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class ChipViewModel : WorkspaceViewModel
{
    private readonly IReadOnlyDictionary<string, LogicFamilyProfile> families;
    private readonly ChipDraftHistory<ChipDefinition> history = new();
    [ObservableProperty] private string chipName = "";
    [ObservableProperty] private string chipDescription = "";
    [ObservableProperty] private string? selectedFamily;
    [ObservableProperty] private string? selectedPackage;
    [ObservableProperty] private string delay = "";
    [ObservableProperty] private string message = "";

    public ChipViewModel(ILogicFamilyRepository? familyRepository = null, IDialogService? dialogs = null,
        IChipCsvRepository? csv = null, IExportRepository? exports = null, IChipTestBenchFactory? bench = null)
        : base(WorkspaceKind.Chip, "Микросхема", "03", "Конструктор микросхем", "")
    {
        var catalog = familyRepository?.Load();
        families = catalog?.Value ?? new Dictionary<string, LogicFamilyProfile>();
        Truth = new(this, dialogs, csv, exports);
        Graph = new(this);
        Bench = new(this, bench);
        Definition = ChipEditorDefaults.Create(families.Keys.FirstOrDefault() ?? "ttl");
        Load(Definition);
        if (catalog is { Success: false }) Message = catalog.Error!;
    }

    public ChipTruthEditorViewModel Truth { get; }
    public ChipGraphEditorViewModel Graph { get; }
    public ChipBenchViewModel Bench { get; }
    public ChipDefinition Definition { get; private set; }
    public IReadOnlyList<ChipPinEdit> Pins { get; private set; } = [];
    public IReadOnlyList<string> Problems { get; private set; } = [];
    public IReadOnlyList<string> Packages => ChipPackage.Supported;
    public IReadOnlyDictionary<string, LogicFamilyProfile> Families => families;
    public IReadOnlyList<ChipPinType> PinTypes { get; } = Enum.GetValues<ChipPinType>();
    public bool IsTruth => Definition.Behavior is ChipTruthTable;
    public bool IsGraph => Definition.Behavior is ChipGraph;

    [RelayCommand]
    private void ApplyPins()
    {
        Edit(() =>
        {
            if (SelectedPackage is not { } package || SelectedFamily is not { } family ||
                !long.TryParse(Delay.Trim(), out var delayUs))
                throw new FormatException("Выберите корпус, семейство и укажите задержку целым числом.");
            if (string.IsNullOrWhiteSpace(ChipName)) throw new FormatException("Введите название микросхемы.");
            var count = ChipPackage.PinCount(package);
            var current = Pins.Select(pin => pin.ToPin()).ToDictionary(pin => pin.Number);
            var pins = Enumerable.Range(1, count)
                .Select(number => current.GetValueOrDefault(number) ?? ChipEditorDefaults.Unused(number)).ToArray();
            Change(Definition with { Name = ChipName.Trim(), Description = ChipDescription.Trim(), Pins = pins,
                Package = package, Family = family, PropagationDelayUs = delayUs });
        });
    }

    [RelayCommand]
    private void NewChip()
    {
        Change(ChipEditorDefaults.Create(families.Keys.FirstOrDefault() ?? "ttl"));
    }

    [RelayCommand]
    private void ShowTruth()
    {
        if (!IsTruth) ChangeBehavior(new ChipTruthTable([]));
    }

    [RelayCommand]
    private void ShowGraph()
    {
        if (!IsGraph) ChangeBehavior(new ChipGraph([], []));
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo() => Load(history.Undo(Definition));
    private bool CanUndo() => history.CanUndo;

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo() => Load(history.Redo(Definition));
    private bool CanRedo() => history.CanRedo;

    internal void Edit(Action action)
    {
        try { Message = ""; action(); }
        catch (Exception error) when (error is FormatException or OverflowException) { Message = error.Message; }
    }

    internal bool CheckReady()
    {
        if (Problems.Count == 0) return true;
        Message = "Исправьте ошибки в определении микросхемы.";
        return false;
    }

    internal IReadOnlyList<string> Validate(ChipDefinition definition) => ChipValidator.Validate(definition, families);
    internal void ChangeBehavior(ChipBehaviorDefinition value) => Change(Definition with { Behavior = value });

    private void Change(ChipDefinition value)
    {
        history.Record(Definition);
        Load(value);
    }

    private void Load(ChipDefinition value)
    {
        Definition = value;
        Bench.Reset();
        ChipName = value.Name;
        ChipDescription = value.Description;
        SelectedFamily = value.Family;
        SelectedPackage = value.Package;
        Delay = value.PropagationDelayUs.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Pins = value.Pins.OrderBy(pin => pin.Number).Select(pin => new ChipPinEdit(pin)).ToArray();
        Problems = Validate(value);
        Truth.Load(value.Behavior as ChipTruthTable ?? new([]));
        Graph.Load(value.Behavior as ChipGraph ?? new([], []));
        OnPropertyChanged(nameof(Definition));
        OnPropertyChanged(nameof(Pins));
        OnPropertyChanged(nameof(Problems));
        OnPropertyChanged(nameof(IsTruth));
        OnPropertyChanged(nameof(IsGraph));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }
}
