using System.ComponentModel;
using CircuitEditor.App.Services;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Core.Simulation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class SimulationViewModel : ObservableObject, IDisposable
{
    private readonly ProjectViewModel project;
    private readonly ISimulationPlayback playback;
    private readonly ISimulationUiPulse pulse;

    public SimulationViewModel(ProjectViewModel project, ISimulationPlayback playback, ISimulationUiPulse pulse, IScenarioRepository scenarios)
    {
        this.project = project;
        this.playback = playback;
        this.pulse = pulse;
        Scenario = new(project, scenarios);
        Controller = new(project, playback);
        Instruments = new(project, playback);
        Controller.Refresh();
        pulse.Tick += OnFrame;
        project.ProjectReplaced += OnProjectReplaced;
        project.ModelCommitted += OnProjectCommitted;
        project.PropertyChanged += OnProjectChanged;
    }

    public SimulationScenarioViewModel Scenario { get; }
    public SimulationControllerViewModel Controller { get; }
    public SimulationInstrumentsViewModel Instruments { get; }
    public bool HasSession => playback.HasSession;
    public bool CanConfigure => !HasSession;
    public bool IsRunning => playback.IsRunning;
    public bool CanStart => !IsRunning && !State.IsHalted && !project.InGesture;
    public bool CanStep => CanStart;
    public double Speed { get => playback.Speed; set { playback.Speed = value; OnPropertyChanged(); } }
    [ObservableProperty] private SimulationState state = new();
    [ObservableProperty] private bool showVoltage = true;
    [ObservableProperty] private bool showCurrent;
    [ObservableProperty] private ComponentInputsViewModel? runtimeInputs;
    [ObservableProperty] private string status = "Сеанс остановлен";
    public event EventHandler<SimulationState>? FrameChanged;
    public event EventHandler? ConfigurationRequested;

    [RelayCommand] private void ShowConfiguration() => ConfigurationRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        if (HasSession) playback.Resume();
        else Begin(true);
        NotifyPlayback();
    }

    [RelayCommand(CanExecute = nameof(IsRunning))]
    private void Pause()
    {
        playback.Pause();
        NotifyPlayback();
    }

    [RelayCommand(CanExecute = nameof(CanStep))]
    private void Step()
    {
        if (HasSession) playback.Step();
        else Begin(false);
        NotifyPlayback();
    }

    [RelayCommand(CanExecute = nameof(HasSession))]
    private void Reset()
    {
        playback.Stop();
        pulse.Stop();
        project.IsSimulationActive = false;
        RuntimeInputs = null;
        SetFrame(new());
        NotifyPlayback();
    }

    private void Begin(bool run)
    {
        project.Wiring.Exit();
        playback.Start(project.Model, Scenario.Configuration(), run);
        Instruments.BeginSession();
        project.IsSimulationActive = true;
        UpdateRuntimeInputs();
        pulse.Start();
    }

    private void NotifyPlayback()
    {
        OnPropertyChanged(nameof(HasSession));
        OnPropertyChanged(nameof(CanConfigure));
        OnPropertyChanged(nameof(IsRunning));
        StartCommand.NotifyCanExecuteChanged();
        PauseCommand.NotifyCanExecuteChanged();
        StepCommand.NotifyCanExecuteChanged();
        ResetCommand.NotifyCanExecuteChanged();
        Controller.SessionChanged();
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var seconds = State.Microseconds / 1_000_000d;
        Status = State.Failure ?? (!HasSession ? "Сеанс остановлен" :
            $"{(IsRunning ? "Симуляция" : "Пауза")} · {seconds:0.000} с");
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        if (playback.TakeFrame() is not { } frame) return;
        SetFrame(frame);
        if (frame.IsHalted)
        {
            project.IsSimulationActive = false;
            NotifyPlayback();
        }
        else UpdateStatus();
    }

    private void SetFrame(SimulationState value)
    {
        State = value;
        project.Model.SetSimulation(value);
        Controller.Update(HasSession ? value : null);
        Instruments.Observe(value);
        FrameChanged?.Invoke(this, value);
    }

    public void Press(Guid component, bool pressed) => playback.Send(s => s.SetInput(component,
        ComponentInput.ButtonPressed, pressed ? 1 : 0));

    public void Switch(Guid component) => playback.Send(session =>
    {
        var item = session.Circuit.Project.Circuit.Components.FirstOrDefault(c => c.Id == component);
        if (item is not null) session.SetInput(component, ComponentInput.SwitchPosition,
            item.Inputs.Values.GetValueOrDefault(ComponentInput.SwitchPosition) == 0 ? 1 : 0);
    });

    public void Turn(Guid component, double angle) => playback.SetLatest(component, s => s.SetPotentiometer(component, angle));

    private void UpdateRuntimeInputs()
    {
        RuntimeInputs = null;
        if (!HasSession || project.SelectedComponent?.Model is not { } part ||
            part.Definition.Id is not ("dht11" or "dht22" or "hc-sr04" or "pir" or "photoresistor" or "thermistor")) return;
        var values = part.Inputs.Values.ToDictionary();
        if (State.Components.GetValueOrDefault(part.Id)?.Measurements is { } measured)
            foreach (var key in values.Keys.ToArray())
                if (measured.TryGetValue(key.ToString(), out var value)) values[key] = value;
        var copy = new CircuitEditor.Core.Models.Component(part.Definition, part.Reference, part.Id, part.Value, part.Color, part.I2cAddress, part.CapacitorKind,
            part.PotentiometerAngle, new ComponentInputs(values), part.Electrical);
        RuntimeInputs = new(copy, input =>
        {
            var value = copy.Inputs.Values[input];
            playback.SetLatest((part.Id, input), s => s.SetInput(part.Id, input, value));
        });
    }

    private void OnProjectChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProjectViewModel.SelectedComponent) &&
            lastSelection != project.SelectedComponent?.Model.Id)
        {
            lastSelection = project.SelectedComponent?.Model.Id;
            UpdateRuntimeInputs();
        }
        if (e.PropertyName == nameof(ProjectViewModel.CanUndo)) NotifyPlayback();
    }

    private Guid? lastSelection;

    private void OnProjectCommitted(object? sender, EventArgs e)
    {
        if (HasSession) Reset();
        Controller.Refresh();
    }

    private void OnProjectReplaced(object? sender, EventArgs e)
    {
        if (HasSession) Reset();
    }

    public void Dispose()
    {
        playback.Stop();
        pulse.Stop();
        pulse.Tick -= OnFrame;
        project.ProjectReplaced -= OnProjectReplaced;
        project.ModelCommitted -= OnProjectCommitted;
        project.PropertyChanged -= OnProjectChanged;
    }
}
