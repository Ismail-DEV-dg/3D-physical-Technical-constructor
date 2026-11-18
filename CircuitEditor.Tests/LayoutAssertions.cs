using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Geometry;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Models;

namespace CircuitEditor.Tests;

internal static class LayoutAssertions
{
    public static void Validate(Component component, ComponentLayout layout)
    {
        var placement = new ComponentPlacement(40, -20);
        var labels = layout.AllLabels.ToArray();
        Assert.True(layout.Body.Contains(layout.Symbol), component.Definition.Name);
        Assert.Equal(component.Definition.Pins.Count(pin => pin.IsExposed), layout.Pins.Count);
        foreach (var label in labels)
        {
            Assert.True(label.FontSize >= 12);
            Assert.True(layout.Body.Contains(label.Bounds), $"{component.Definition.Name}: outside {label.Text}");
            Assert.True(label.Bounds.X >= layout.Body.X + 6);
            Assert.True(label.Bounds.Right <= layout.Body.Right - 6);
            Assert.True(label.Bounds.Y > layout.Body.Y && label.Bounds.Bottom < layout.Body.Bottom);
            Assert.False(label.Bounds.Intersects(layout.Symbol), $"{component.Definition.Name}: symbol / {label.Text}");
        }
        for (var first = 0; first < labels.Length; first++)
            for (var second = first + 1; second < labels.Length; second++)
                Assert.False(labels[first].Bounds.Intersects(labels[second].Bounds),
                    $"{component.Definition.Name}: {labels[first].Text} / {labels[second].Text}");
        foreach (var pin in layout.Pins)
        {
            Assert.Equal(0, pin.Position.X % DiagramGeometry.GridSize, 8);
            Assert.Equal(0, pin.Position.Y % DiagramGeometry.GridSize, 8);
            Assert.True(pin.IsLeft ? pin.Label.Bounds.X - layout.Body.X >= 6
                : layout.Body.Right - pin.Label.Bounds.Right >= 6);
            var line = new LayoutRect(Math.Min(pin.Position.X, pin.IsLeft ? layout.Body.X : layout.Body.Right),
                pin.Position.Y - 0.7, ComponentLayoutStyle.LeadLength, 1.4);
            Assert.All(labels, label => Assert.False(label.Bounds.Intersects(line)));
        }
        foreach (var side in layout.Pins.GroupBy(pin => pin.IsLeft))
        {
            var rows = side.OrderBy(pin => pin.Position.Y).ToArray();
            for (var row = 1; row < rows.Length; row++)
                Assert.Equal(layout.PinPitch, rows[row].Position.Y - rows[row - 1].Position.Y, 8);
        }
        for (var quarter = 0; quarter < 4; quarter++)
        {
            foreach (var pin in layout.Pins)
            {
                var world = LayoutTransform.ToWorld(placement, layout, pin.Position);
                Assert.Equal(Math.Round(world.X / DiagramGeometry.GridSize), world.X / DiagramGeometry.GridSize, 8);
                Assert.Equal(Math.Round(world.Y / DiagramGeometry.GridSize), world.Y / DiagramGeometry.GridSize, 8);
                var local = LayoutTransform.ToLocal(placement, layout, world);
                Assert.Equal(pin.Position.X, local.X, 8);
                Assert.Equal(pin.Position.Y, local.Y, 8);
            }
            placement.Rotate();
        }
    }
}
