using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Models;

namespace CircuitEditor.Simulation.Components;

public sealed class NpnBehavior : IComponentBehavior
{
    public string ComponentType => "npn";

    public IEnumerable<DcBranch> Branches(BehaviorContext context, Component c)
    {
        foreach (var branch in context.Branch(c, "база", "B", "E", "LED",
                     context.Parameter(c, "baseOhms"), context.Parameter(c, "baseVolts")))
            yield return branch;
        var state = context.For(c);
        if (state.CollectorLimit <= 0) yield break;
        var branches = state.AlternateCollector
            ? context.Branch(c, "коллектор", "C", "E", "I", state.CollectorLimit)
            : context.Branch(c, "коллектор", "C", "E", "LED", context.Parameter(c, "saturationOhms"),
                context.Parameter(c, "saturationVolts"));
        foreach (var branch in branches) yield return branch;
    }

    public bool Update(BehaviorContext context, Component c)
    {
        const double tolerance = 1e-7;
        var memory = context.For(c);
        var current = context.Currents(c).ToArray();
        var limit = Math.Max(0, current.FirstOrDefault(b => b.Name.EndsWith(".база", StringComparison.Ordinal))?.Current ?? 0)
            * context.Parameter(c, "gain");
        var collector = current.FirstOrDefault(b => b.Name.EndsWith(".коллектор", StringComparison.Ordinal));
        var limited = memory.AlternateCollector
            ? collector is not null && collector.Voltage > context.Parameter(c, "saturationVolts") +
                limit * context.Parameter(c, "saturationOhms")
            : collector?.Current > limit + tolerance;
        if (Math.Abs(memory.CollectorLimit - limit) < tolerance && memory.AlternateCollector == limited) return false;
        memory.CollectorLimit = limit;
        memory.AlternateCollector = limited;
        return true;
    }
}
