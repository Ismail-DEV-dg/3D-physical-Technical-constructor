using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Simulation.Components;

public abstract class DisplayBehavior(string componentType, bool parallel = false) : IComponentBehavior
{
    public string ComponentType => componentType;

    public ComponentSignal Observe(BehaviorContext context, Component c)
    {
        var powered = parallel ? context.Powered(c, "VDD", "VSS") : context.Powered(c);
        if (!powered) return new();
        var text = context.For(c).Text ?? "";
        var columns = (int)context.Parameter(c, "columns");
        var rows = (int)context.Parameter(c, "rows");
        var lines = text.Replace("\r", "").Split('\n').SelectMany(line => line.Length == 0
            ? new[] { "" } : line.Chunk(columns).Select(chars => new string(chars))).Take(rows);
        return new(Text: string.Join('\n', lines), Active: true);
    }
}
