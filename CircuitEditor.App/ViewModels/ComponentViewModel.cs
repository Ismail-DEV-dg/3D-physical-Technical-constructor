using CircuitEditor.Core.Models;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Resistance;
using CircuitEditor.Core.Quantities;
using CircuitEditor.Core.Inputs;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CircuitEditor.App.ViewModels;

public sealed partial class ComponentViewModel : ObservableObject
{
    private readonly IComponentLayoutCalculator layoutCalculator;
    private readonly CircuitProject project;
    private readonly Core.Electronics.ElectricalCatalog? electricalCatalog;
    public ComponentViewModel(Component model, CircuitProject project, FootprintDefinition footprint,
        IComponentLayoutCalculator layoutCalculator, Core.Electronics.ElectricalCatalog? electricalCatalog=null)
    {
        this.electricalCatalog=electricalCatalog;
        this.layoutCalculator = layoutCalculator; this.project = project;
        Model = model; Footprint = footprint; Layout = layoutCalculator.Calculate(model);
        CreateElectricalEditor();
        editValue = model.Value; editColor = model.Color ?? ""; editI2cAddress = model.I2cAddress ?? "";
        if (IsResistor) ResistorEditor = new ResistorEditorViewModel(model.Value, ApplyResistance);
        if (model.Definition.Id is "capacitor" or "inductor")
            NominalEditor = new PassiveNominalEditorViewModel(IsCapacitor ? PassiveQuantity.Capacitance : PassiveQuantity.Inductance,
                model.Value, ApplyNominal);
        if (model.Definition.Id == "potentiometer") PotentiometerEditor = new(model, NotifyAppearance);
        if (ComponentInputDefinitions.For(model.Definition.Id).Count > 0) InputsEditor = new(model, InputChanged);
    }
    public Component Model { get; }
    public FootprintDefinition Footprint { get; }
    public ComponentPlacement SchematicPlacement => project.SchematicLayout.Get(Model.Id);
    public ComponentPlacement MockupPlacement => project.MockupLayout.Get(Model.Id);
    public ComponentLayout Layout { get; private set; }
    public ElectricalPropertiesViewModel? ElectricalEditor {get;private set;}
    private void CreateElectricalEditor()
    {
        ElectricalEditor=electricalCatalog is not null && Model.Definition.Id is "led" or "rgb-led" or "photoresistor" or "thermistor" ? new(Model,electricalCatalog,NotifyMutation) : null;
        OnPropertyChanged(nameof(ElectricalEditor));
    }
    private void NotifyMutation(){MutationVersion++;OnPropertyChanged(nameof(MutationVersion));}
    public void UpdateLayout(){Layout = layoutCalculator.Calculate(Model);CreateElectricalEditor();}
    public string Reference => Model.Reference;
    public string Name => Model.Definition.Name;
    public string Description => Model.Definition.Description;
    public double Voltage => Model.Definition.Id == "battery" ? Model.Inputs.Values[ComponentInput.BatteryVoltage] : Model.Definition.Voltage;
    public ComponentInputsViewModel? InputsEditor { get; }
    public bool ShowsGenericValue => Model.Definition.Id is "led" or "potentiometer" or "diode" or "npn" or "relay" or "buzzer";
    public bool HasImmediateProperties => !ShowsGenericValue && !SupportsI2c && !ShowsColor;
    public bool ShowsFixedNtcValue => Model.Definition.Id == "thermistor";
    public int MutationVersion { get; private set; }
    private void InputChanged(ComponentInput input)
    {
        MutationVersion++; OnPropertyChanged(nameof(MutationVersion));
        if (input == ComponentInput.BatteryVoltage) OnPropertyChanged(nameof(Voltage));
        if (input is ComponentInput.BatteryVoltage or ComponentInput.RgbCommon) { EditValue = Model.Value; UpdateLayout(); }
        if (ComponentInputDefinitions.ChangesAppearance(input)) NotifyAppearance();
    }
    public bool IsResistor => Model.Definition.Id == "resistor";
    public bool IsCapacitor => Model.Definition.Id == "capacitor";
    public bool HasListedNominal => IsResistor || NominalEditor is not null;
    public PassiveNominalEditorViewModel? NominalEditor { get; }
    public PotentiometerEditorViewModel? PotentiometerEditor { get; }
    public CapacitorKind SelectedCapacitorKind
    {
        get => Model.CapacitorKind;
        set { if (value == Model.CapacitorKind || !Model.TrySetCapacitorKind(value)) return;
            OnPropertyChanged(); NotifyAppearance(); }
    }
    public int AppearanceVersion { get; private set; }
    private void NotifyAppearance() { AppearanceVersion++; OnPropertyChanged(nameof(AppearanceVersion)); }
    private void ApplyNominal(string value)
    {
        if (!Model.TryUpdateProperties(value, Model.Color, Model.I2cAddress, out _)) return;
        EditValue = value; UpdateLayout(); NotifyAppearance();
    }
    public bool ShowsColor => Model.Definition.Id == "led";
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
            (pin.IsInputOnly ? " / IN only" : "") + (pin.IsNoConnect ? " / NC: не подключён" : "") + (!pin.IsExposed ? " / не выведен" : "");
    }
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private string editValue;
    [ObservableProperty] private string editColor;
    [ObservableProperty] private string editI2cAddress;
}
