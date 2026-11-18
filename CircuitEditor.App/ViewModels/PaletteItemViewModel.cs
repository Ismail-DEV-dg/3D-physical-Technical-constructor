namespace CircuitEditor.App.ViewModels;

public sealed class PaletteItemViewModel(string name, string category, string symbol, string description)
{
    public string Name { get; } = name;
    public string Category { get; } = category;
    public string Symbol { get; } = symbol;
    public string Description { get; } = description;
}

