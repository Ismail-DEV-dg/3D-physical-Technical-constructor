using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Instruments;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class ChipBenchViewModel(ChipViewModel editor, IChipTestBenchFactory? factory) : ObservableObject
{
    private ChipTestBench? session;
    [ObservableProperty] private ChipPin? clockPin;
    [ObservableProperty] private InstrumentSignal? trace;
    [ObservableProperty] private int page;
    [ObservableProperty] private string result = "";
    [ObservableProperty] private IReadOnlyList<string> differences = [];
    public bool IsActive => session is not null;
    public InstrumentKind TraceKind => InstrumentKind.Logic;
    public IReadOnlyList<ChipPin> ClockPins => session?.Channels.Where(pin => pin.Reads).ToArray() ?? [];
    public int PageCount => session is { } current
        ? Math.Max(1, (current.Channels.Count + ChipTestBench.ChannelCount - 1) / ChipTestBench.ChannelCount) : 0;
    public string ChannelLabel => session is { } current ? string.Join("   ", current.Channels
        .Skip(Page * ChipTestBench.ChannelCount).Take(ChipTestBench.ChannelCount)
        .Select((pin, index) => $"{index + 1} · {pin.Name}")) : "";
    public IReadOnlyList<ChipBenchPinViewModel> Inputs =>
        session is { } current ? current.Channels.Where(pin => pin.Reads).Select(pin => new ChipBenchPinViewModel(pin,
            current.Inputs[pin.Number], new RelayCommand(() => Toggle(pin.Number)))).ToArray() : [];
    public IReadOnlyList<ChipBenchPinViewModel> Outputs =>
        session is { } current ? current.Channels.Where(pin => pin.Drives).Select(pin =>
            new ChipBenchPinViewModel(pin, current.Outputs[pin.Number], null)).ToArray() : [];

    [RelayCommand]
    private void Start()
    {
        if (factory is null || !editor.CheckReady()) return;
        session = factory.Create(editor.Definition);
        ClockPin = ClockPins.FirstOrDefault(pin => pin.Name.Contains("CLK", StringComparison.OrdinalIgnoreCase))
            ?? ClockPins.FirstOrDefault();
        Page = 0;
        Result = "";
        Differences = [];
        Refresh();
    }

    [RelayCommand]
    private void Pulse()
    {
        if (ClockPin is { } pin) session?.Pulse(pin.Number);
        Refresh();
    }

    [RelayCommand]
    private void NextTrace()
    {
        if (PageCount > 0) Page = (Page + 1) % PageCount;
        Refresh();
    }

    [RelayCommand]
    private void Verify()
    {
        if (factory is null || !editor.CheckReady()) return;
        if (editor.Definition.Behavior is not ChipTruthTable table)
        {
            Result = "Автопроверка по эталону доступна в режиме таблицы истинности.";
            Differences = [];
            return;
        }
        Differences = ChipBenchVerification.Verify(editor.Definition, table, factory);
        Result = Differences.Count == 0 ? "Автопроверка пройдена." : $"Автопроверка не пройдена. Расхождений: {Differences.Count}.";
    }

    internal void Reset()
    {
        session = null;
        ClockPin = null;
        Page = 0;
        Result = "";
        Differences = [];
        Refresh();
    }

    private void Toggle(int pin)
    {
        if (session is not { } current) return;
        current.SetInput(pin, current.Inputs[pin] == ChipLevel.High ? ChipLevel.Low : ChipLevel.High);
        Refresh();
    }

    private void Refresh()
    {
        Trace = session?.Trace(Page * ChipTestBench.ChannelCount);
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(Inputs));
        OnPropertyChanged(nameof(Outputs));
        OnPropertyChanged(nameof(ClockPins));
        OnPropertyChanged(nameof(ChannelLabel));
    }
}
