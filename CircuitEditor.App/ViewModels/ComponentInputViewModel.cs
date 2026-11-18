using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CircuitEditor.App.ViewModels;

public sealed class ComponentInputViewModel : ObservableObject
{
    private readonly Component model;
    private readonly Action<ComponentInput> changed;
    public ComponentInputViewModel(Component model, InputDefinition definition, Action<ComponentInput> changed)
    {
        this.model = model; Definition = definition; this.changed = changed;
        Choices = definition.Choices?.Select(value => new InputChoiceViewModel(value, definition.Format(value))).ToArray() ?? [];
    }
    public InputDefinition Definition { get; }
    public IReadOnlyList<InputChoiceViewModel> Choices { get; }
    public bool IsChoice => Choices.Count > 0;
    public string DisplayValue => Definition.Format(Value);
    public double Value
    {
        get => model.Inputs.Values[Definition.Input];
        set
        {
            if (value == Value || !model.TrySetInput(Definition.Input, value)) return;
            OnPropertyChanged(); OnPropertyChanged(nameof(DisplayValue)); changed(Definition.Input);
        }
    }
}
