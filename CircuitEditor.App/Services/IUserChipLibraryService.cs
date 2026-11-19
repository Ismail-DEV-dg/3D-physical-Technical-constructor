using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Models;

namespace CircuitEditor.App.Services;

public interface IUserChipLibraryService
{
    event EventHandler? Changed;
    IReadOnlyList<ChipDefinition> Definitions { get; }
    IReadOnlyList<ComponentDefinition> Palette { get; }
    ComponentDefinition? ComponentFor(ChipDefinition definition);
    string? Refresh();
}
