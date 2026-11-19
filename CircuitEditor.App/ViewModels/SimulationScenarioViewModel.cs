using System.Collections.Immutable;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Gpio;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CircuitEditor.App.ViewModels;

public sealed partial class SimulationScenarioViewModel : ObservableObject
{
    private readonly ProjectViewModel project;
    public SimulationScenarioViewModel(ProjectViewModel project, IScenarioRepository repository)
    {
        this.project = project;
        Templates = [new("", "Без сценария", new Dictionary<string,string>(), new Dictionary<string,double>()),
            ..repository.Load().Value ?? []];
        template = Templates[0];
        project.ModelCommitted += (_, _) => Rebuild(Configuration());
    }

    public IReadOnlyList<ScenarioTemplate> Templates { get; }
    [ObservableProperty] private ScenarioTemplate template;
    [ObservableProperty] private string text = "";
    [ObservableProperty] private ComponentViewModel? target;
    [ObservableProperty] private IReadOnlyList<ScenarioPinViewModel> bindings = [];
    [ObservableProperty] private IReadOnlyList<SimulationNumberViewModel> numbers = [];
    [ObservableProperty] private IReadOnlyList<ComponentViewModel> targets = [];
    public bool ShowsText => Template.Text is not null;
    public bool ShowsTarget => Template.Target is not null;
    partial void OnTemplateChanged(ScenarioTemplate value)
    {
        if (value is null) return;
        Rebuild(null);
        OnPropertyChanged(nameof(ShowsText));
        OnPropertyChanged(nameof(ShowsTarget));
    }

    internal ScenarioConfiguration? Configuration() => string.IsNullOrEmpty(Template.Id) ? null : new(
        Template.Id, Bindings.Where(p => p.Selected is not null).ToImmutableDictionary(p => p.Name, p => p.Selected!.Reference),
        Numbers.ToImmutableDictionary(p => p.Name, p => p.Value), Target?.Model.Id, Text);

    private void Rebuild(ScenarioConfiguration? retained)
    {
        var options = project.Components.Where(c => c.Model.Definition.Id is "arduino-uno" or "arduino-nano" or "esp32-devkit")
            .SelectMany(c => c.Model.Definition.Pins.Where(GpioController.IsGpio).Select(p => new SimulationPinOption(
                new(c.Model.Id, p.Id), $"{c.Reference}.{p.Id}"))).ToArray();
        Bindings = Template.Pins.Select(p =>
        {
            var original = retained?.Pins.GetValueOrDefault(p.Key);
            var choices = options;
            if (original is not null && options.All(o => o.Reference != original))
                choices = [..options, new(original, $"{original.PinId} (удалён)")];
            return new ScenarioPinViewModel(p.Key, choices) { Selected = choices.FirstOrDefault(o => o.Reference == original) };
        }).ToArray();
        Numbers = Template.Numbers.Select(p => new SimulationNumberViewModel(p.Key,
            retained?.Numbers.GetValueOrDefault(p.Key, p.Value) ?? p.Value)).ToArray();
        Targets = project.Components.Where(c => Template.Target switch
        {
            "display" => c.Model.Definition.Id is "lcd1602" or "lcd1602-i2c" or "oled-ssd1306",
            "sensor" => c.Model.Definition.Id is "dht11" or "dht22" or "pir" or "hc-sr04" or "thermistor" or "photoresistor",
            _ => false
        }).ToArray();
        Target = Targets.FirstOrDefault(c => c.Model.Id == retained?.TargetId);
        Text = retained?.Text ?? Template.Text ?? "";
    }
}
