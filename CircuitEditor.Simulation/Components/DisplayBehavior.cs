using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Simulation.Components;

public sealed class DisplayBehavior : IComponentBehavior
{
    public IReadOnlyList<string> ComponentTypes => ["lcd1602", "lcd1602-i2c", "oled-ssd1306"];

    public ComponentSignal Observe(BehaviorContext context, Component c)
    {
        var powered = c.Definition.Id == "lcd1602" ? context.Powered(c, "VDD", "VSS") : context.Powered(c);
        if (!powered) return new();
        var text = context.For(c).Text ?? "";
        var columns = (int)context.Parameter(c, "columns");
        var rows = (int)context.Parameter(c, "rows");
        var lines = text.Replace("\r", "").Split('\n').SelectMany(line => line.Length == 0
            ? new[] { "" } : line.Chunk(columns).Select(chars => new string(chars))).Take(rows);
        return new(Text: string.Join('\n', lines), Active: true);
    }
}
