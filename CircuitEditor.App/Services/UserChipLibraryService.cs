using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Repositories;

namespace CircuitEditor.App.Services;

public sealed class UserChipLibraryService : IUserChipLibraryService
{
    private readonly IChipRepository storage;
    private readonly IReadOnlyDictionary<string, LogicFamilyProfile> families;

    public UserChipLibraryService(IChipRepository storage, ILogicFamilyRepository profiles)
    {
        this.storage = storage;
        families = profiles.Load().Value ?? new Dictionary<string, LogicFamilyProfile>();
        Refresh();
    }

    public event EventHandler? Changed;
    public IReadOnlyList<ChipDefinition> Definitions { get; private set; } = [];
    public IReadOnlyList<ComponentDefinition> Palette { get; private set; } = [];

    public ComponentDefinition? ComponentFor(ChipDefinition definition) =>
        families.TryGetValue(definition.Family, out var family) ? ChipPackage.Component(definition, family) : null;

    public string? Refresh()
    {
        var result = storage.List();
        if (result.Value is null) return result.Error;
        Definitions = result.Value;
        Palette = Definitions.Where(chip => families.ContainsKey(chip.Family))
            .Select(chip => ChipPackage.Component(chip, families[chip.Family])).ToArray();
        Changed?.Invoke(this, EventArgs.Empty);
        return result.Warning;
    }
}
