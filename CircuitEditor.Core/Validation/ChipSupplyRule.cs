using CircuitEditor.Core.Chips;

namespace CircuitEditor.Core.Validation;

/// <summary>Проверяет внешнее подключение всех выводов питания пользовательской микросхемы.</summary>
public sealed class ChipSupplyRule : IErcRule
{
    /// <inheritdoc/>
    public IEnumerable<ErcIssue> Check(ErcContext context)
    {
        foreach (var component in context.Project.Circuit.Components)
        {
            if (context.Project.FindChip(component.Definition.Id) is not { } chip) continue;
            var missing = chip.Pins.Where(pin => pin.Type is ChipPinType.Vcc or ChipPinType.Gnd)
                .Select(pin => ChipPackage.PinId(pin.Number)).Where(id => !context.Nets.Connected(component.Id, id)).ToArray();
            if (missing.Length == 0) continue;
            yield return ErcContext.Issue("CHIP_UNPOWERED", $"{component.Reference}: питание микросхемы не подключено.",
                missing.Select(pin => new Models.PinReference(component.Id, pin)),
                missing.Select(pin => context.Nets.Find(component.Id, pin)?.Id).OfType<string>(), ErcSeverity.Warning);
        }
    }
}
