using CircuitEditor.App.Services;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Gpio;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class SimulationControllerViewModel(ProjectViewModel project, ISimulationPlayback playback) : ObservableObject
{
    public static IReadOnlyList<GpioMode> Modes { get; } = Enum.GetValues<GpioMode>();
    [ObservableProperty] private IReadOnlyList<SimulationPinOption> pins = [];
    [ObservableProperty] private SimulationPinOption? pin;
    [ObservableProperty] private GpioMode mode;
    [ObservableProperty] private bool high;
    [ObservableProperty] private double duty = .5;
    [ObservableProperty] private bool overrideAnalog;
    [ObservableProperty] private double analog;
    [ObservableProperty] private double pwmPeriodUs;
    [ObservableProperty] private string reading = "";
    public bool HasSession => playback.HasSession;
    public bool CanApply => HasSession && Pin is not null;
    private SimulationState? last;

    internal void Refresh()
    {
        var previous = Pin?.Reference;
        Pins = project.Components.Where(c => c.Model.Definition.Id is "arduino-uno" or "arduino-nano" or "esp32-devkit")
            .SelectMany(c => c.Model.Definition.Pins.Where(GpioController.IsGpio).Select(p => new SimulationPinOption(
                new(c.Model.Id, p.Id), $"{c.Reference}.{p.Id}"))).ToArray();
        Pin = Pins.FirstOrDefault(p => p.Reference == previous) ?? Pins.FirstOrDefault();
    }

    internal void SessionChanged()
    {
        OnPropertyChanged(nameof(HasSession));
        ApplyCommand.NotifyCanExecuteChanged();
    }

    internal void Update(SimulationState? state)
    {
        last = state;
        var signal = Pin is { } p ? last?.Pins.GetValueOrDefault(p.Reference) : null;
        Reading = signal is null ? "" : $"Уровень {signal.Level} · {signal.Voltage?.ToString("0.###") ?? "—"} В · АЦП {signal.Adc?.ToString() ?? "—"}";
    }

    partial void OnPinChanged(SimulationPinOption? value)
    {
        ApplyCommand.NotifyCanExecuteChanged();
        Update(last);
    }

    [RelayCommand(CanExecute = nameof(CanApply))]
    private void Apply()
    {
        if (Pin is not { } pin) return;
        var state = new GpioState(Mode, High, Duty, OverrideAnalog ? Analog : null,
            PwmPeriodUs > 0 && PwmPeriodUs <= long.MaxValue ? (long)PwmPeriodUs : null);
        playback.Send(s => s.SetPin(pin.Reference, state));
    }
}
