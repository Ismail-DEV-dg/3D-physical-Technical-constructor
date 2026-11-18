using CircuitEditor.Core.Models;

namespace CircuitEditor.App.ViewModels;

public sealed class PaletteItemViewModel(ComponentDefinition definition)
{
    public ComponentDefinition Definition { get; } = definition;
    public string Name => Definition.Name;
    public string Category => Definition.Category;
    public string Symbol => Definition.Prefix;
    public string Description => Definition.Description;
}
