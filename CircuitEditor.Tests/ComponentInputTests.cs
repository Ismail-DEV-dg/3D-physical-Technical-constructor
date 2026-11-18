using System.Text.Json;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Models;

namespace CircuitEditor.Tests;

public sealed class ComponentInputTests
{
    [Theory]
    [InlineData("dht11", ComponentInput.Temperature, 0, 50, "50 °C")]
    [InlineData("dht11", ComponentInput.Humidity, 20, 90, "90 % RH")]
    [InlineData("dht22", ComponentInput.Temperature, -40, 80, "80 °C")]
    [InlineData("dht22", ComponentInput.Humidity, 0, 100, "100 % RH")]
    [InlineData("hc-sr04", ComponentInput.Distance, 2, 400, "400 см")]
    [InlineData("photoresistor", ComponentInput.Illuminance, 0, 100, "100 %")]
    [InlineData("thermistor", ComponentInput.Temperature, -55, 125, "125 °C")]
    public void DatasheetRangesFormattingAndRejectionAreCentralized(string id, ComponentInput parameter,
        double minimum, double maximum, string formatted)
    {
        var project = BreadboardTestData.Create(); var item = BreadboardTestData.Add(project, id);
        var definition = ComponentInputDefinitions.For(id).Single(value => value.Input == parameter);
        Assert.Equal(minimum, definition.Minimum); Assert.Equal(maximum, definition.Maximum);
        Assert.True(item.TrySetInput(parameter, minimum)); Assert.True(item.TrySetInput(parameter, maximum));
        Assert.Equal(formatted, definition.Format(item.Inputs.Values[parameter]));
        var before = JsonSerializer.Serialize(item);
        foreach (var invalid in new[] { minimum - 1, maximum + 1, double.NaN, double.NegativeInfinity })
        {
            Assert.False(item.TrySetInput(parameter, invalid));
            Assert.Equal(before, JsonSerializer.Serialize(item));
        }
    }
    [Theory]
    [InlineData("pir", ComponentInput.Motion)]
    [InlineData("rgb-led", ComponentInput.RgbCommon)]
    [InlineData("switch", ComponentInput.SwitchPosition)]
    public void BinaryPropertiesOnlyAcceptTheirChoices(string id, ComponentInput parameter)
    {
        var project = BreadboardTestData.Create(); var item = BreadboardTestData.Add(project, id);
        Assert.True(item.TrySetInput(parameter, 1)); Assert.Equal(1, item.Inputs.Values[parameter]);
        Assert.False(item.TrySetInput(parameter, .5)); Assert.False(item.TrySetInput(parameter, 2));
        Assert.True(item.TrySetInput(parameter, 0));
    }
    [Fact]
    public void BatteryAcceptsExactlyRequestedVoltages()
    {
        var project = BreadboardTestData.Create(); var item = BreadboardTestData.Add(project, "battery");
        var expected = new[] { 1.5, 3, 3.7, 4.5, 5, 9 };
        Assert.Equal(expected, ComponentInputDefinitions.For("battery")[0].Choices);
        foreach (var voltage in expected) Assert.True(item.TrySetInput(ComponentInput.BatteryVoltage, voltage));
        Assert.False(item.TrySetInput(ComponentInput.BatteryVoltage, 6));
        Assert.True(item.TrySetInput(ComponentInput.BatteryVoltage, 3.7)); Assert.Equal("3.7 В", item.Value);
        Assert.Equal(9, item.Definition.Voltage); // Library rating and electrical calculations are unchanged.
    }
    [Fact]
    public void InputsRoundTripStayIndependentAndDoNotRunSimulation()
    {
        var project = BreadboardTestData.Create();
        foreach (var id in new[] { "dht11", "dht22", "hc-sr04", "pir", "photoresistor", "thermistor", "rgb-led", "switch", "battery" })
        {
            var item = BreadboardTestData.Add(project, id);
            foreach (var definition in ComponentInputDefinitions.For(id)) Assert.True(item.TrySetInput(definition.Input, definition.Maximum));
        }
        var first = project.Circuit.Components.First(); var duplicate = BreadboardTestData.Add(project, "dht11");
        Assert.NotEqual(first.Inputs.Values[ComponentInput.Temperature], duplicate.Inputs.Values[ComponentInput.Temperature]);
        Assert.False(duplicate.TrySetInput(ComponentInput.Distance, 100));
        var json = JsonSerializer.Serialize(project);
        Assert.Equal(json, JsonSerializer.Serialize(JsonSerializer.Deserialize<CircuitProject>(json)));
        Assert.Empty(project.Circuit.Wires);
        var state = new Dictionary<ComponentInput, double> { [ComponentInput.Distance] = 20 };
        var snapshot = new ComponentInputs(state); state[ComponentInput.Distance] = 300;
        Assert.Equal(20, snapshot.Values[ComponentInput.Distance]);
        Assert.Throws<ArgumentException>(() => new Component(duplicate.Definition, "S99", Guid.NewGuid(), "DHT11", "#00E5FF", null, inputs: snapshot));
    }
    [Fact]
    public void SensorEditingDoesNotInvalidateStaticDrawing()
    {
        using var project = TestProjectFactory.Create(); using var scene = TestProjectFactory.Mockup(project);
        var item = project.Add(project.Library.Single(item => item.Id == "dht22"))!;
        var before = scene.Revision; var value = item.Model.Value;
        item.InputsEditor!.Parameters.Single(parameter => parameter.Definition.Input == ComponentInput.Temperature).Value = -12.3;
        Assert.Equal(-12.3, item.Model.Inputs.Values[ComponentInput.Temperature]);
        Assert.Equal(before, scene.Revision); Assert.Equal(value, item.Model.Value);
    }
}
