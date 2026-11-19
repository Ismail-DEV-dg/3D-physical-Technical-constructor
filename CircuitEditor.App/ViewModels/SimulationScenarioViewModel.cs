using System.Collections.Immutable;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Gpio;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CircuitEditor.App.ViewModels;

public sealed partial class SimulationScenarioViewModel : ObservableObject, IDisposable
{
    private readonly ProjectViewModel project;
    private readonly ScenarioTemplate[] library;
    private bool refreshing;

    public SimulationScenarioViewModel(ProjectViewModel project, IScenarioRepository repository)
    {
        this.project = project;
        library = [new("", "Без сценария", new Dictionary<string,string>(), new Dictionary<string,double>()),
            ..repository.Load().Value ?? []];
        template = library[0];
        Restore();
        project.ModelCommitted += OnModelCommitted;
    }

    [ObservableProperty] private IReadOnlyList<ScenarioTemplate> templates = [];
    [ObservableProperty] private ScenarioTemplate template;
    [ObservableProperty] private string text = "";
    [ObservableProperty] private SimulationTargetOption? target;
    [ObservableProperty] private IReadOnlyList<ScenarioPinViewModel> bindings = [];
    [ObservableProperty] private IReadOnlyList<SimulationNumberViewModel> numbers = [];
    [ObservableProperty] private IReadOnlyList<SimulationTargetOption> targets = [];
    public bool ShowsText => Template?.Text is not null;
    public bool ShowsTarget => Template?.Target is not null;

    partial void OnTemplateChanged(ScenarioTemplate value)
    {
        if (!refreshing && value is not null)
        {
            refreshing = true;
            try { Rebuild(null); } finally { refreshing = false; }
            Changed();
        }
        OnPropertyChanged(nameof(ShowsText));
        OnPropertyChanged(nameof(ShowsTarget));
    }

    partial void OnTextChanged(string value) => Changed();
    partial void OnTargetChanged(SimulationTargetOption? value) => Changed();

    internal ScenarioConfiguration? Configuration() => string.IsNullOrEmpty(Template?.Id) ? null : new(
        Template.Id, Bindings.Where(p => p.Selected is not null).ToImmutableDictionary(p => p.Name, p => p.Selected!.Reference),
        Numbers.ToImmutableDictionary(p => p.Name, p => p.Value), Target?.Id, Template.Text is null ? null : Text);

    private void Restore()
    {
        var setup = project.Model.SimulationSetup.Scenario;
        refreshing = true;
        try
        {
            var choice = setup is null ? library[0] : library.FirstOrDefault(t => t.Id == setup.TemplateId);
            var available = library;
            if (choice is null && setup is not null)
            {
                choice = new(setup.TemplateId, $"{setup.TemplateId} (недоступен)",
                    setup.Pins.Keys.ToDictionary(k => k, _ => "input"), setup.Numbers, setup.Text,
                    setup.TargetId is null ? null : "unknown");
                available = [..library, choice];
            }
            Templates = available;
            Template = choice ?? library[0];
            Rebuild(setup);
        }
        finally { refreshing = false; }
    }

    private void Rebuild(ScenarioConfiguration? retained)
    {
        foreach (var row in Bindings) row.PropertyChanged -= RowChanged;
        foreach (var row in Numbers) row.PropertyChanged -= RowChanged;
        var options = project.Components.Where(c => c.Model.Definition.Id is "arduino-uno" or "arduino-nano" or "esp32-devkit")
            .SelectMany(c => c.Model.Definition.Pins.Where(GpioController.IsGpio).Select(p => new SimulationPinOption(
                new(c.Model.Id, p.Id), $"{c.Reference}.{p.Id}"))).ToArray();
        Bindings = Template.Pins.Select(p =>
        {
            var original = retained?.Pins.GetValueOrDefault(p.Key);
            var choices = options;
            if (original is not null && options.All(o => o.Reference != original))
                choices = [..options, new(original, $"{original.PinId} (удалён)")];
            var row = new ScenarioPinViewModel(p.Key, choices) { Selected = choices.FirstOrDefault(o => o.Reference == original) };
            row.PropertyChanged += RowChanged;
            return row;
        }).ToArray();
        Numbers = Template.Numbers.Select(p =>
        {
            var row = new SimulationNumberViewModel(p.Key, retained?.Numbers.GetValueOrDefault(p.Key, p.Value) ?? p.Value);
            row.PropertyChanged += RowChanged;
            return row;
        }).ToArray();
        var targets = project.Components.Where(c => Template.Target switch
        {
            "display" => c.Model.Definition.Id is "lcd1602" or "lcd1602-i2c" or "oled-ssd1306",
            "sensor" => c.Model.Definition.Id is "dht11" or "dht22" or "pir" or "hc-sr04" or "thermistor" or "photoresistor",
            _ => false
        }).Select(c => new SimulationTargetOption(c.Model.Id, $"{c.Reference} · {c.Name}")).ToList();
        if (Template.Target is not null) targets.Insert(0, new(null, "Без назначения"));
        if (retained?.TargetId is Guid id && targets.All(c => c.Id != id)) targets.Add(new(id, "Компонент удалён"));
        Targets = targets;
        Target = Targets.FirstOrDefault(c => c.Id == retained?.TargetId);
        Text = retained?.Text ?? Template.Text ?? "";
    }

    private void Changed()
    {
        if (refreshing) return;
        var setup = project.Model.SimulationSetup with { Scenario = Configuration() };
        if (setup.IsValid) project.UpdateSimulationSetup(setup);
    }

    private void RowChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => Changed();
    private void OnModelCommitted(object? sender, EventArgs e) => Restore();

    public void Dispose()
    {
        project.ModelCommitted -= OnModelCommitted;
        foreach (var row in Bindings) row.PropertyChanged -= RowChanged;
        foreach (var row in Numbers) row.PropertyChanged -= RowChanged;
    }
}
