using CircuitEditor.App.Services;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Core.Wiring;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CircuitEditor.App.ViewModels;

public sealed partial class SimulationInstrumentsViewModel : ObservableObject
{
    private readonly ProjectViewModel project;
    private readonly ISimulationPlayback playback;
    private InstrumentViewModel? picking;
    private int pickingChannel;

    public SimulationInstrumentsViewModel(ProjectViewModel project, ISimulationPlayback playback)
    {
        this.project = project;
        this.playback = playback;
        Meter = new(InstrumentKind.Meter, "Мультиметр", 12, 12, BeginPick, Assign);
        Scope = new(InstrumentKind.Scope, "Осциллограф", 30, 72, BeginPick, Assign);
        Logic = new(InstrumentKind.Logic, "Логический анализатор", 58, 112, BeginPick, Assign);
        Panels = [Meter, Scope, Logic];
        foreach (var panel in Panels)
            panel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(InstrumentViewModel.IsVisible)) return;
                if (!panel.IsVisible && picking == panel) CancelPick();
                for (var i = 0; i < panel.Probes.Length; i++) Assign(panel.Kind, i, panel.Probes[i]);
            };
    }

    public InstrumentViewModel Meter { get; }
    public InstrumentViewModel Scope { get; }
    public InstrumentViewModel Logic { get; }
    public IReadOnlyList<InstrumentViewModel> Panels { get; }
    [ObservableProperty] private string hint = "";

    internal void BeginSession()
    {
        foreach (var panel in Panels.Where(p => p.IsVisible))
            for (var i = 0; i < panel.Probes.Length; i++)
                if (panel.Probes[i] is { } terminal) Assign(panel.Kind, i, terminal);
    }

    internal void Observe(SimulationState state)
    {
        foreach (var panel in Panels) panel.Signal = state.Instruments.GetValueOrDefault(panel.Kind);
    }

    public bool CancelPick()
    {
        if (picking is null) return false;
        picking.IsPicking = false;
        picking = null;
        Hint = "";
        return true;
    }

    public bool TryPick(LayoutPoint position)
    {
        if (picking is null) return false;
        if (WireGeometry.Nearest(project.Model, position) is not { } end)
        {
            Hint = "Зонд: укажите контакт компонента или отверстие breadboard.";
            return true;
        }
        picking.SetProbe(pickingChannel, end);
        CancelPick();
        return true;
    }

    private void BeginPick(InstrumentViewModel panel)
    {
        CancelPick();
        if (panel.Channel < 0) return;
        picking = panel;
        pickingChannel = panel.Channel;
        panel.IsPicking = true;
        Hint = $"{panel.Name}: выберите точку для {panel.Channels[pickingChannel]}. Esc — отмена.";
    }

    private void Assign(InstrumentKind kind, int channel, WireEndpoint? endpoint)
    {
        var panel = Panels.Single(p => p.Kind == kind);
        var usable = panel.IsVisible && endpoint is not null && WireGeometry.Resolve(project.Model, endpoint) is not null ? endpoint : null;
        playback.SetLatest((kind, channel), s => s.AssignProbe(kind, channel, usable));
    }
}
