using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.Validation;
/// <summary>Питание и земля в одной идеальной цепи.</summary>
public sealed class SupplyShortRule : IErcRule
{
    /// <inheritdoc/>
    public IEnumerable<ErcIssue> Check(ErcContext c) => c.Nets.Nets.Where(n=>n.Pins.Any(p=>c.Pin(p).Type==PinType.Ground)
        && n.Pins.Any(p=>c.Pin(p).Type==PinType.Power)).Select(n=>ErcContext.Issue("SUPPLY_SHORT","КЗ: питание и земля в одном узле.",n.Pins,[n.Id]));
}
