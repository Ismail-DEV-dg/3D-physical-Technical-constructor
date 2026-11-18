using System.Windows.Media;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;

namespace CircuitEditor.App.Controls;

// View-owned vector resources, not a model/service or a storage repository.
internal sealed class ComponentDrawingCache
{
    private const int MaximumEntries = 128;
    private readonly Dictionary<(FootprintDefinition Footprint, string Value, string Color, int Span, double Pitch), DrawingGroup> drawings = [];

    public DrawingGroup Get(ComponentViewModel item, ComponentPlacement pose, double pitch)
    {
        var footprint = item.Footprint;
        var span = footprint.IsFlexible ? pose.SpanHoles == 0 ? footprint.DefaultSpan : pose.SpanHoles : 0;
        var value = footprint.Renderer == "axial-resistor" ? item.Model.Value : "";
        var color = footprint.Renderer == "led" ? item.Model.Color : "";
        var key = (footprint, value, color, span, pitch);
        if (drawings.TryGetValue(key, out var cached)) return cached;
        var drawing = new DrawingGroup();
        using (var context = drawing.Open())
            RealComponentDrawing.DrawBody(context, item, new ComponentPlacement(spanHoles: span), pitch);
        drawing.Freeze();
        if (drawings.Count >= MaximumEntries) drawings.Clear();
        drawings.Add(key, drawing);
        return drawing;
    }

    public void Clear() => drawings.Clear();
}
