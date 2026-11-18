using CircuitEditor.Core.Placement;
using CircuitEditor.Infrastructure.Repositories;
using CircuitEditor.Core.Geometry;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Repositories;

namespace CircuitEditor.Tests;

public sealed class CircuitModelTests
{
    private static ComponentDefinition Definition(string id = "resistor") =>
        new JsonComponentLibraryRepository().Load().Components.Single(item => item.Id == id);

    [Fact]
    public void ComponentsHaveIndependentStateAndMonotonicReferences()
    {
        var circuit = new Circuit();
        var resistor = Definition();
        var first = circuit.AddComponent(resistor);
        var second = circuit.AddComponent(resistor);
        Assert.Equal("R1", first.Reference);
        Assert.Equal("R2", second.Reference);
        Assert.NotEqual(first.Id, second.Id);
        Assert.True(first.TryUpdateProperties("1 kΩ", "#39ff88", null, out _));
        Assert.Equal(resistor.DefaultValue, second.Value);
        Assert.Equal("220 Ω", resistor.DefaultValue);
        circuit.RemoveComponent(first.Id);
        Assert.Equal("R3", circuit.AddComponent(resistor).Reference);
        Assert.Equal("LED1", circuit.AddComponent(Definition("led")).Reference);
    }

    [Fact]
    public void FailedAdditionDoesNotConsumeReference()
    {
        var circuit = new Circuit();
        Assert.Throws<ArgumentNullException>(() => circuit.AddComponent(null!));
        Assert.Equal("R1", circuit.AddComponent(Definition()).Reference);
    }

    [Fact]
    public void FourQuarterTurnsRestoreOrientationAndMovementRejectsNonFiniteCoordinates()
    {
        var placement = new ComponentPlacement();
        placement.Rotate();
        Assert.Equal(90, placement.Rotation);
        for (var i = 0; i < 3; i++) placement.Rotate();
        Assert.Equal(0, placement.Rotation);
        Assert.Throws<ArgumentException>(() => placement.MoveTo(0, double.PositiveInfinity));
        Assert.Equal(0, placement.Y);
    }

    [Theory]
    [InlineData("", "#39FF88", null)]
    [InlineData("220 Ω", "red", null)]
    [InlineData("220 Ω", "#GG0000", null)]
    [InlineData("220 Ω", "#39FF88", "0x27")]
    public void InvalidPropertiesLeaveComponentUnchanged(string value, string color, string? address)
    {
        var component = new Component(new JsonComponentLibraryRepository().Load().Components.Single(d => d.Id == "led"), "LED1");
        var previous = component.Value; var previousColor = component.Color;
        Assert.False(component.TryUpdateProperties(value, color, address, out var error));
        Assert.NotNull(error);
        Assert.Equal(previous, component.Value);
        Assert.Equal(previousColor, component.Color);
    }

    [Theory]
    [InlineData("0x08", true)]
    [InlineData("0x77", true)]
    [InlineData("0x07", false)]
    [InlineData("0x78", false)]
    [InlineData("0x80", false)]
    [InlineData("27", false)]
    [InlineData(null, false)]
    public void I2cAddressUsesSevenBitUnreservedRange(string? address, bool expected)
    {
        var component = new Component(Definition("lcd1602-i2c"), "LCD1");
        Assert.Equal(expected, component.TryUpdateProperties("Display", "#8B5CFF", address, out _));
    }

    [Fact]
    public void WiresRequireExistingExposedPinsRejectDuplicatesAndCascadeOnDelete()
    {
        var circuit = new Circuit();
        var first = circuit.AddComponent(Definition());
        var second = circuit.AddComponent(Definition());
        var start = new PinReference(first.Id, "1");
        var end = new PinReference(second.Id, "2");
        Assert.True(circuit.TryAddWire(new Wire(start, end)));
        Assert.False(circuit.TryAddWire(new Wire(end, start)));
        Assert.False(circuit.TryAddWire(new Wire(start, new PinReference(second.Id, "missing"))));
        Assert.False(circuit.TryAddWire(new Wire(start, new PinReference(Guid.NewGuid(), "1"))));
        var esp = circuit.AddComponent(Definition("esp32-devkit"));
        Assert.False(circuit.TryAddWire(new Wire(start, new PinReference(esp.Id, "GPIO37"))));
        circuit.RemoveComponent(second.Id);
        Assert.Empty(circuit.Wires);
        Assert.Throws<ArgumentException>(() => new Wire(start, start));
    }

    [Fact]
    public void HitTestAndMarqueeUseRotatedBounds()
    {
        var placement = new ComponentPlacement(20, 20);
        Assert.True(DiagramGeometry.Contains(placement, 25, 25, 160, 100));
        placement.Rotate();
        Assert.False(DiagramGeometry.Contains(placement, 25, 25, 160, 100));
        Assert.True(DiagramGeometry.Contains(placement, 100, 70, 160, 100));
        Assert.True(DiagramGeometry.Intersects(placement, 90, -20, 110, 0, 160, 100));
        Assert.Equal(20, DiagramGeometry.Snap(29));
        Assert.Equal(-20, DiagramGeometry.Snap(-11));
    }
}
