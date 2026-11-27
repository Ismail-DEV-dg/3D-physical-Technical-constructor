using CircuitEditor.Core.Models;
using CircuitEditor.Simulation.Components;
using CircuitEditor.Simulation.Engine;

namespace CircuitEditor.Simulation.Sketches;

internal sealed class SketchDisplayOutput(SimulationCircuit circuit, BehaviorContext context)
{
    private readonly Dictionary<Guid, Component> displays = circuit.Project.Circuit.Components
        .Where(part => part.Definition.Id is "lcd1602" or "lcd1602-i2c" or "oled-ssd1306")
        .ToDictionary(part => part.Id);

    internal void Write(Guid id, string text, bool newLine)
    {
        var display = displays[id];
        var memory = context.For(display);
        var oled = display.Definition.Id == "oled-ssd1306";
        var columns = (int)context.Parameter(display, "columns");
        var limit = columns * (int)context.Parameter(display, "rows");
        var existing = (oled ? memory.DisplayBuffer : memory.Text) ?? "";
        var cursor = memory.DisplayCursor;
        var length = Math.Min(text.Length, limit - cursor);
        if (length > 0)
        {
            var end = cursor + length;
            var prefix = existing.Length < cursor ? existing.PadRight(cursor) : existing[..cursor];
            var suffix = end < existing.Length ? existing.AsSpan(end) : ReadOnlySpan<char>.Empty;
            var output = string.Concat(prefix, text.AsSpan(0, length), suffix);
            if (oled) memory.DisplayBuffer = output;
            else memory.Text = output;
            cursor = end;
        }
        var lastColumn = length > 0 ? cursor - 1 : cursor;
        memory.DisplayCursor = newLine && cursor < limit ? Math.Min(limit, (lastColumn / columns + 1) * columns) : cursor;
    }

    internal void Clear(Guid id)
    {
        var display = displays[id];
        var memory = context.For(display);
        if (display.Definition.Id == "oled-ssd1306") memory.DisplayBuffer = "";
        else
        {
            memory.Text = "";
            memory.DisplayCursor = 0;
        }
    }

    internal void Cursor(Guid id, int position) => context.For(displays[id]).DisplayCursor = position;

    internal void Flush(Guid id)
    {
        var memory = context.For(displays[id]);
        memory.Text = memory.DisplayBuffer;
    }
}
