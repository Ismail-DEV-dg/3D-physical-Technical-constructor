using CircuitEditor.Core.Chips;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class ChipGraphEditorViewModel(ChipViewModel editor) : ObservableObject
{
    private ChipGraph graph = new([], []);
    [ObservableProperty] private ChipGate? selectedGate;
    [ObservableProperty] private ChipOutputLink? selectedLink;
    [ObservableProperty] private ChipGateKind gateKind;
    [ObservableProperty] private string gatePorts = "";
    [ObservableProperty] private string gateClear = "";
    [ObservableProperty] private string gatePreset = "";
    [ObservableProperty] private string outputPin = "";
    [ObservableProperty] private string outputSource = "";

    public IReadOnlyList<ChipGateKind> GateKinds { get; } = Enum.GetValues<ChipGateKind>();
    public IReadOnlyList<ChipGate> Gates => graph.Gates;
    public IReadOnlyList<ChipOutputLink> Links => graph.Outputs;
    public ChipGraph Graph => graph;
    public string PortGuide => GateKind switch
    {
        ChipGateKind.Not => "Один источник.",
        ChipGateKind.D => "Порядок: D, CLK. CLEAR / PRESET активны уровнем 1.",
        ChipGateKind.Jk => "Порядок: J, K, CLK. CLEAR / PRESET активны уровнем 1.",
        ChipGateKind.Rs => "Порядок: S, R. Оба активны уровнем 1.",
        _ => "От 2 до 8 источников."
    };

    internal void Load(ChipGraph value)
    {
        var gate = SelectedGate?.Id;
        var output = SelectedLink?.Pin;
        graph = value;
        OnPropertyChanged(nameof(Gates));
        OnPropertyChanged(nameof(Links));
        OnPropertyChanged(nameof(Graph));
        SelectedGate = graph.Gates.FirstOrDefault(item => item.Id == gate);
        SelectedLink = graph.Outputs.FirstOrDefault(item => item.Pin == output);
    }

    partial void OnGateKindChanged(ChipGateKind value) => OnPropertyChanged(nameof(PortGuide));

    partial void OnSelectedGateChanged(ChipGate? value)
    {
        if (value is not { } gate) return;
        GateKind = gate.Kind;
        GatePorts = string.Join(", ", gate.Inputs.Select(ChipEditorText.Signal));
        GateClear = ChipEditorText.Signal(gate.Clear);
        GatePreset = ChipEditorText.Signal(gate.Preset);
    }

    partial void OnSelectedLinkChanged(ChipOutputLink? value)
    {
        if (value is not { } link) return;
        OutputPin = ChipPackage.PinId(link.Pin);
        OutputSource = ChipEditorText.Signal(link.Source);
    }

    [RelayCommand]
    private void AddGate() => EditGate(false);

    [RelayCommand]
    private void ApplyGate() { if (SelectedGate is not null) EditGate(true); }

    private void EditGate(bool replace)
    {
        editor.Edit(() =>
        {
            if (!replace && graph.Gates.Count == ChipGraphValidation.MaximumGates)
                throw new FormatException("Достигнут предел числа блоков.");
            var used = graph.Gates.Select(gate => gate.Id).ToHashSet();
            var id = replace ? SelectedGate!.Id : Enumerable.Range(1, used.Count + 1).First(number => !used.Contains(number));
            var ports = GatePorts.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(ChipEditorText.ParseSignal).ToArray();
            var gate = new ChipGate(id, GateKind, ports, Optional(GateClear), Optional(GatePreset));
            editor.ChangeBehavior(graph with { Gates = graph.Gates.Where(item => item.Id != id).Append(gate).OrderBy(item => item.Id).ToArray() });
            SelectedGate = gate;
        });
    }

    [RelayCommand]
    private void RemoveGate()
    {
        if (SelectedGate is not { } gate) return;
        editor.ChangeBehavior(new ChipGraph(graph.Gates.Where(item => item.Id != gate.Id)
            .Select(item => item with
            {
                Inputs = item.Inputs.Where(wire => wire.Gate != gate.Id).ToArray(),
                Clear = item.Clear?.Gate == gate.Id ? null : item.Clear,
                Preset = item.Preset?.Gate == gate.Id ? null : item.Preset
            }).ToArray(), graph.Outputs.Where(link => link.Source.Gate != gate.Id).ToArray()));
    }

    [RelayCommand]
    private void Connect()
    {
        editor.Edit(() =>
        {
            var pin = ChipEditorText.ParseNumber(OutputPin);
            if (!editor.Definition.Pins.Any(item => item.Number == pin && item.Drives))
                throw new FormatException($"Вывод {pin} не настроен как выход.");
            var link = new ChipOutputLink(pin, ChipEditorText.ParseSignal(OutputSource));
            editor.ChangeBehavior(graph with { Outputs = graph.Outputs.Where(item => item.Pin != pin)
                .Append(link).OrderBy(item => item.Pin).ToArray() });
            SelectedLink = link;
        });
    }

    [RelayCommand]
    private void Disconnect()
    {
        if (SelectedLink is { } link)
            editor.ChangeBehavior(graph with { Outputs = graph.Outputs.Where(item => item.Pin != link.Pin).ToArray() });
    }

    private static ChipSignal? Optional(string text) => string.IsNullOrWhiteSpace(text) ? null : ChipEditorText.ParseSignal(text);
}
