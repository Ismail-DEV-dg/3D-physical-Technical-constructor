using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Models;

namespace CircuitEditor.App.ViewModels;

public sealed class ComponentInputsViewModel(Component model, Action<ComponentInput> changed)
{
    public IReadOnlyList<ComponentInputViewModel> Parameters { get; } = ComponentInputDefinitions.For(model.Definition.Id)
        .Select(definition => new ComponentInputViewModel(model, definition, changed)).ToArray();
}
