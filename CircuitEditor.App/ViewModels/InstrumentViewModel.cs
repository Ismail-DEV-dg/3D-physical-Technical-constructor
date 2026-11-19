using CircuitEditor.Core.Simulation;
using CircuitEditor.Core.Wiring;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class InstrumentViewModel : ObservableObject
{
    private readonly Action<InstrumentViewModel> pick;
    private readonly Action<InstrumentKind, int, WireEndpoint?> changeProbe;

    public InstrumentViewModel(InstrumentKind kind, string name, double left, double top, Action<InstrumentViewModel> pick,
        Action<InstrumentKind, int, WireEndpoint?> changeProbe)
    {
        Kind = kind;
        Name = name;
        DefaultLayout = new(left, top, false);
        Left = left;
        Top = top;
        this.pick = pick;
        this.changeProbe = changeProbe;
        var count = kind == InstrumentKind.Logic ? 8 : 2;
        Probes = new WireEndpoint?[count];
        Channels = kind == InstrumentKind.Meter ? ["Красный (+)", "Чёрный (−)"] : Enumerable.Range(1, count)
            .Select(i => $"CH {i}").ToArray();
    }

    public InstrumentKind Kind { get; }
    internal InstrumentLayout DefaultLayout { get; }
    internal InstrumentLayout Layout => new(Left, Top, IsVisible);
    public string Name { get; }
    public IReadOnlyList<string> Channels { get; }
    internal WireEndpoint?[] Probes { get; }
    public bool IsMeter => Kind == InstrumentKind.Meter;
    public bool IsScope => Kind == InstrumentKind.Scope;
    public bool HasTrace => !IsMeter;
    public IReadOnlyList<string> MeterModes { get; } = ["Напряжение", "Ток"];
    public IReadOnlyList<string> Triggers { get; } = ["Авто", "↑ CH 1", "↓ CH 1"];
    public IReadOnlyList<double> VoltsChoices { get; } = [.2, .5, 1, 2, 5];
    public IReadOnlyList<double> TimeChoices { get; } = [.2, .5, 1, 2, 5, 10, 20, 40];
    [ObservableProperty] private bool isVisible;
    [ObservableProperty] private double left;
    [ObservableProperty] private double top;
    [ObservableProperty] private int channel;
    [ObservableProperty] private double voltsPerDivision = 1;
    [ObservableProperty] private double millisecondsPerDivision = 5;
    [ObservableProperty] private double triggerLevel = 2;
    [ObservableProperty] private int triggerMode;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Reading))] private int meterMode;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Reading))] private InstrumentSignal? signal;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ProbeLabel))] private bool isPicking;

    public string Reading => MeterMode == 0
        ? Signal?.Volts is double v ? $"{v:0.###} В" : "— В"
        : Signal?.Amps is double a ? $"{a * 1000:0.###} мА" : "— мА";

    public string ProbeLabel => IsPicking ? "Щёлкните по контакту или отверстию" :
        Channel >= 0 && Channel < Probes.Length && Probes[Channel] is { } end ? $"Зонд: {end.TerminalId}" : "Зонд не задан";

    public event EventHandler? LayoutChanged;

    partial void OnChannelChanged(int value) => OnPropertyChanged(nameof(ProbeLabel));
    partial void OnIsVisibleChanged(bool value) => LayoutChanged?.Invoke(this, EventArgs.Empty);

    [RelayCommand] private void SelectProbe() => pick(this);

    [RelayCommand] private void ClearProbe() => SetProbe(Channel, null);

    [RelayCommand] private void Close() => IsVisible = false;

    [RelayCommand]
    private void Move(InstrumentMove move)
    {
        Left = Math.Clamp(Left + move.DeltaX, 0, Math.Max(0, move.MaximumX));
        Top = Math.Clamp(Top + move.DeltaY, 0, Math.Max(0, move.MaximumY));
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    internal void Restore(InstrumentLayout? saved)
    {
        var layout = saved ?? DefaultLayout;
        Left = layout.X;
        Top = layout.Y;
        IsVisible = layout.Visible;
        for (var i = 0; i < Probes.Length; i++) SetProbe(i, null);
        Signal = null;
    }

    internal void SetProbe(int channel, WireEndpoint? terminal)
    {
        if (channel < 0 || channel >= Probes.Length) return;
        Probes[channel] = terminal;
        changeProbe(Kind, channel, terminal);
        OnPropertyChanged(nameof(ProbeLabel));
    }
}
