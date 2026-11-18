using CircuitEditor.Infrastructure.Repositories;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Repositories;

namespace CircuitEditor.Tests;

public sealed class ComponentLibraryTests
{
    [Fact]
    public void EmbeddedLibraryContainsEveryRequestedComponent()
    {
        var result = new JsonComponentLibraryRepository().Load();
        Assert.True(result.Success, result.Error);
        string[] expected = ["arduino-uno", "arduino-nano", "esp32-devkit", "resistor", "capacitor",
            "inductor", "potentiometer", "led", "rgb-led", "lcd1602", "lcd1602-i2c", "oled-ssd1306",
            "seven-segment", "buzzer", "button", "switch", "battery", "photoresistor", "thermistor",
            "dht11", "dht22", "hc-sr04", "pir", "servo", "relay", "npn", "diode"];
        Assert.Equal(expected.Order(), result.Components.Select(item => item.Id).Order());
        foreach (var component in result.Components)
        {
            Assert.False(string.IsNullOrWhiteSpace(component.Description));
            Assert.True(component.Voltage > 0);
            Assert.NotEmpty(component.Pins);
            Assert.All(component.Pins, pin => Assert.True(Enum.IsDefined(pin.Type)));
            Assert.Equal(component.Pins.Count, component.Pins.Select(pin => pin.Id).Distinct().Count());
        }
    }

    [Fact]
    public void Esp32InputOnlyPinsArePreservedAndNotAssignedPwm()
    {
        var board = new JsonComponentLibraryRepository().Load().Components.Single(item => item.Id == "esp32-devkit");
        for (var gpio = 34; gpio <= 39; gpio++)
        {
            var pin = board.Pins.Single(pin => pin.Id == $"GPIO{gpio}");
            Assert.True(pin.IsInputOnly);
            Assert.DoesNotContain(PinType.PWM, pin.Capabilities);
            Assert.Equal(gpio is not 37 and not 38, pin.IsExposed);
        }
        Assert.Equal(3.3, board.Voltage);
        Assert.False(board.Pins.Single(pin => pin.Id == "GPIO6").IsExposed);
    }

    [Fact]
    public void ArduinoPinsIncludeAnalogPwmAndBusCapabilities()
    {
        var library = new JsonComponentLibraryRepository().Load().Components;
        foreach (var id in new[] { "arduino-uno", "arduino-nano" })
        {
            var board = library.Single(item => item.Id == id);
            Assert.Contains(PinType.I2C_SDA, board.Pins.Single(pin => pin.Id == "A4").Capabilities);
            Assert.Contains(PinType.I2C_SCL, board.Pins.Single(pin => pin.Id == "A5").Capabilities);
            foreach (var number in new[] { 3, 5, 6, 9, 10, 11 })
                Assert.Contains(PinType.PWM, board.Pins.Single(pin => pin.Id == $"D{number}").Capabilities);
        }
        Assert.True(library.Single(item => item.Id == "arduino-nano").Pins.Single(pin => pin.Id == "A7").IsInputOnly);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[null]")]
    [InlineData("{ broken")]
    [InlineData("[{}]")]
    public void MalformedLibraryReturnsErrorWithoutPartialData(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var result = JsonComponentLibraryRepository.Read(stream);
        Assert.False(result.Success);
        Assert.NotEmpty(result.Error!);
        Assert.Empty(result.Components);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("pin-type")]
    [InlineData("pin-bounds")]
    [InlineData("unknown-property")]
    [InlineData("voltage")]
    public void InvalidRecordsAreRejectedAtomically(string fault)
    {
        var definition = new JsonComponentLibraryRepository().Load().Components.First();
        var node = JsonSerializer.SerializeToNode(definition)!.AsObject();
        if (fault == "pin-type") node["Pins"]![0]!["Type"] = "NotAPinType";
        if (fault == "pin-bounds") node["Pins"]![0]!["X"] = -1;
        if (fault == "unknown-property") node["Typo"] = "bad";
        if (fault == "voltage") node["Voltage"] = -1;
        // Serialised enums are numeric by default; make all known values explicit string names.
        foreach (var pin in node["Pins"]!.AsArray())
        {
            if (pin!["Type"] is JsonValue value && value.TryGetValue<int>(out var number))
                pin["Type"] = ((PinType)number).ToString();
            var capabilities = pin["Capabilities"]!.AsArray();
            for (var i = 0; i < capabilities.Count; i++)
                capabilities[i] = ((PinType)capabilities[i]!.GetValue<int>()).ToString();
        }
        var entries = new JsonArray(node);
        if (fault == "duplicate") entries.Add(node.DeepClone());
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(entries.ToJsonString()));
        var result = JsonComponentLibraryRepository.Read(stream);
        Assert.False(result.Success);
        Assert.Empty(result.Components);
    }

    [Fact]
    public void JsonRoundTripPreservesPinsAndDefaults()
    {
        var original = new JsonComponentLibraryRepository().Load().Components.Single(item => item.Id == "lcd1602-i2c");
        var options = new JsonSerializerOptions
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter<PinType>() }
        };
        using var stream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new[] { original }, options));
        var loaded = JsonComponentLibraryRepository.Read(stream);
        Assert.True(loaded.Success, loaded.Error);
        var copy = Assert.Single(loaded.Components);
        Assert.Equal(original.DefaultI2cAddress, copy.DefaultI2cAddress);
        Assert.Equal(original.Pins.Select(pin => pin.Type), copy.Pins.Select(pin => pin.Type));
    }
}
