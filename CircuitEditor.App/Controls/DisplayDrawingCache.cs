using System.Windows.Media;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.App.Controls;

internal sealed class DisplayDrawingCache
{
    private const int MaximumEntries = 128;
    private readonly Dictionary<Guid, (FootprintDefinition Footprint, string Text, DrawingGroup Drawing)> drawings = [];

    public void Draw(DrawingContext context, Guid component, FootprintDefinition footprint, ComponentSignal signal)
    {
        if (!signal.Active) return;
        var text = signal.Text ?? "";
        if (!drawings.TryGetValue(component, out var entry) || !ReferenceEquals(entry.Footprint, footprint) || entry.Text != text)
        {
            var drawing = new DrawingGroup();
            using (var display = drawing.Open())
                SimulationDrawing.Display(display, PhysicalDrawing.Rect(footprint.Body), text, footprint.Renderer == "oled-ssd1306");
            drawing.Freeze();
            if (drawings.Count >= MaximumEntries && !drawings.ContainsKey(component)) drawings.Clear();
            drawings[component] = entry = (footprint, text, drawing);
        }
        context.DrawDrawing(entry.Drawing);
    }

    public void Clear() => drawings.Clear();
}
