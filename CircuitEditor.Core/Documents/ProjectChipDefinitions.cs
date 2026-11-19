using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Wiring;

namespace CircuitEditor.Core.Documents;

/// <summary>Проверяет определения, принадлежащие проекту, и безопасно подставляет локальную ревизию.</summary>
public static class ProjectChipDefinitions
{
    internal static IReadOnlyDictionary<string, ChipDefinition> Read(ProjectDocument document,
        IReadOnlyDictionary<string, LogicFamilyProfile> profiles)
    {
        Dictionary<string, ChipDefinition> chips = new(StringComparer.Ordinal);
        foreach (var chip in document.EmbeddedChips)
        {
            if (chip is null) throw new ArgumentException("В проекте отсутствует встроенное определение микросхемы.");
            var errors = ChipValidator.Validate(chip, profiles);
            if (errors.Count > 0) throw new ArgumentException($"Микросхема «{chip.Name}»: {errors[0]}");
            if (!chips.TryAdd(chip.ComponentId, chip))
                throw new ArgumentException($"Встроенная ревизия {chip.ComponentId} повторяется.");
        }
        return chips;
    }

    internal static void CheckReference(ComponentData component, IReadOnlyDictionary<string, ChipDefinition> embedded)
    {
        if (component.ChipId is null && component.DefinitionVersion is null && !embedded.ContainsKey(component.DefinitionId)) return;
        if (!embedded.TryGetValue(component.DefinitionId, out var chip) ||
            component.ChipId != chip.Id || component.DefinitionVersion != chip.DefinitionVersion || component.FootprintId != chip.ComponentId)
            throw new ArgumentException($"{component.Reference}: повреждена ссылка на встроенную микросхему.");
    }

    /// <summary>Переназначает экземпляры одной ревизии на совместимую локальную версию.</summary>
    public static ProjectDocument Replace(ProjectDocument document, ChipDefinition original, ChipDefinition local)
    {
        if (local.Id != original.Id) throw new ArgumentException("ID локальной копии отличается.");
        var affected = document.Components.Where(part => part.DefinitionId == original.ComponentId).ToArray();
        if (affected.Length == 0) return document;
        var owners = affected.Select(part => part.Id).ToHashSet();
        var wired = document.Wires.SelectMany(wire => new[] { wire.Start, wire.End })
            .Where(end => end.Kind == WireEndpointKind.ComponentContact && owners.Contains(end.OwnerId))
            .Select(end => end.TerminalId);
        var bound = document.Bindings.Where(binding => owners.Contains(binding.ComponentId)).Select(binding => binding.ContactId);
        var originalPins = original.Pins.ToDictionary(pin => ChipPackage.PinId(pin.Number));
        var localPins = local.Pins.ToDictionary(pin => ChipPackage.PinId(pin.Number));
        foreach (var id in wired.Concat(bound).Distinct(StringComparer.Ordinal))
        {
            if (!originalPins.TryGetValue(id, out var before) || !localPins.TryGetValue(id, out var after)
                || before.Type != after.Type || before.IsUnused != after.IsUnused)
                throw new ArgumentException($"Несовместимый контакт {id}: вывод исчез или изменилась его электрическая роль.");
        }
        var versions = document.EmbeddedChips.Where(chip => chip.ComponentId != original.ComponentId).ToArray();
        var duplicate = versions.FirstOrDefault(chip => chip.ComponentId == local.ComponentId);
        if (duplicate is not null && !ChipContent.Matches(duplicate, local))
            throw new ArgumentException("В проекте уже есть другая копия этой же локальной ревизии.");
        return document with
        {
            EmbeddedChips = duplicate is null ? [.. versions, local] : versions,
            Components = document.Components.Select(part => owners.Contains(part.Id) ? part with
            {
                DefinitionId = local.ComponentId, FootprintId = local.ComponentId, ChipId = local.Id,
                DefinitionVersion = local.DefinitionVersion, Value = local.Package
            } : part).ToArray()
        };
    }
}
