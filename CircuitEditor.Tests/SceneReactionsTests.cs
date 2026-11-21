using CircuitEditor.App.Controls.ThreeD;
using CircuitEditor.Core.Scene;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

public sealed class SceneReactionsTests
{
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(.31, 5)]
    [InlineData(.32, 5)]
    [InlineData(1, 15)]
    [InlineData(4, 15)]
    public void IntensityIsQuantizedToSixteenLevels(double intensity, int level) =>
        Assert.Equal(level, SceneSignalStyle.Brightness(intensity));

    [Fact]
    public async Task RepeatedIntensityBandAndDisplayTextReuseSameFrozenModels()
    {
        var result = await Task.Run(() =>
        {
            var project = BreadboardTestData.Create();
            var led = BreadboardTestData.Add(project, "led");
            var lcd = BreadboardTestData.Add(project, "lcd1602-i2c");
            var scene = new SceneBuilder(new JsonSceneCatalogRepository()).Build(project);
            var prepared = new ScenePreparation(new SceneMeshFactory()).Build(scene, default);
            SimulationState state = new()
            {
                Components = System.Collections.Immutable.ImmutableDictionary<Guid, ComponentSignal>.Empty
                    .Add(led.Id, new(.31)).Add(lcd.Id, new(Text: "Arduino", Active: true))
            };
            var builder = new SceneReactionBuilder();
            var first = builder.Build(prepared.Nodes, state, true, default);
            var next = builder.Build(prepared.Nodes, state with
            {
                Components = state.Components.SetItem(led.Id, new(.32))
            }, true, default);
            return (first, next);
        });
        Assert.Equal(2, result.first.Count);
        Assert.All(result.first.Zip(result.next), pair =>
        {
            Assert.Same(pair.First, pair.Second);
            Assert.True(pair.Second.Model.IsFrozen);
            Assert.True(pair.Second.Model.Material.IsFrozen);
        });
    }
}
