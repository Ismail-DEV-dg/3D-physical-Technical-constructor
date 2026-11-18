using CircuitEditor.Core.Models;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Resistance;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CircuitEditor.App.ViewModels;

public sealed partial class ComponentViewModel : ObservableObject
{
    private readonly IComponentLayoutCalculator layoutCalculator;
    private readonly CircuitProject project;
    public ComponentViewModel(Component model, CircuitProject project, FootprintDefinition footprint,
        IComponentLayoutCalculator layoutCalculator)
    {
        this.layoutCalculator = layoutCalculator; this.project = project;
        Model = model; Footprint = footprint; Layout = layoutCalculator.Calculate(model);
        editValue = model.Value; editColor = model.Color; editI2cAddress = model.I2cAddress ?? "";
        if (IsResistor) ResistorEditor = new ResistorEditorViewModel(model.Value, ApplyResistance);
    }
    public Component Model { get; }
    public FootprintDefinition Footprint { get; }
    public ComponentPlacement SchematicPlacement => project.SchematicLayout.Get(Model.Id);
    public ComponentPlacement MockupPlacement => project.MockupLayout.Get(Model.Id);
    public ComponentLayout Layout { get; private set; }
    public void UpdateLayout() => Layout = layoutCalculator.Calculate(Model);
    public string Reference => Model.Reference;
    public string Name => Model.Definition.Name;
    public string Description => Model.Definition.Description;
    public double Voltage => Model.Definition.Voltage;
    public bool IsResistor => Model.Definition.Id == "resistor";
    public bool ShowsColor => Model.Definition.Id is "led" or "rgb-led";
    public bool ShowsVoltage => Model.Definition.Pins.Any(pin => pin.Type == PinType.Power);
    public ResistorEditorViewModel? ResistorEditor { get; }
    public string ResistanceValue => Model.Value;
    private void ApplyResistance(decimal ohms)
    {
        var value = ResistanceFormatter.Format(ohms);
        if (!Model.TryUpdateProperties(value, Model.Color, Model.I2cAddress, out _)) return;
        EditValue = value; UpdateLayout();
        OnPropertyChanged(nameof(ResistanceValue));
    }
    public bool SupportsI2c => Model.Definition.DefaultI2cAddress is not null;
    public string PinSummary => string.Join("\n", Model.Definition.Pins.Select(DescribePin));
    private static string DescribePin(Pin pin)
    {
        var alternatives = string.Join(", ", pin.Capabilities.Where(type => type != pin.Type));
        return $"{pin.Name}: {pin.Type}{(alternatives.Length > 0 ? " + " + alternatives : "")}" +
            (pin.IsInputOnly ? " / IN only" : "") + (!pin.IsExposed ? " / не выведен" : "");
    }
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private string editValue;
    [ObservableProperty] private string editColor;
    [ObservableProperty] private string editI2cAddress;
}
