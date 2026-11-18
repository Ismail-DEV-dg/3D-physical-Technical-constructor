using System.IO;
using System.Text;
using System.Text.Json;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

public sealed class FootprintRepositoryTests
{
    [Fact]
    public void All27FootprintsHaveMatchingElectricalContactsAndRoundTrip()
    {
        var library = new JsonComponentLibraryRepository().Load().Components;
        var result = new JsonFootprintRepository().Load();
        Assert.True(result.Success, result.Error);
        Assert.Equal(27, result.Value!.Count);
        foreach (var footprint in result.Value)
        {
            var definition = library.Single(item => item.Id == footprint.ComponentDefinitionId);
            Assert.All(footprint.Contacts, contact => Assert.Contains(definition.Pins,
                pin => pin.Id == contact.ElectricalPinId && pin.IsExposed));
        }
        using var stream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(
            new { schemaVersion = 1, units = "mm", footprints = result.Value }));
        var restored = JsonFootprintRepository.Read(stream);
        Assert.True(restored.Success, restored.Error);
        Assert.Equal(27, restored.Value!.Count);
        Assert.Equal(4, restored.Value.Single(item => item.ComponentDefinitionId == "resistor").MinSpan);
        Assert.Equal(4, restored.Value.Single(item => item.ComponentDefinitionId == "button").Contacts.Count);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"schemaVersion\":1,\"units\":\"mm\",\"footprints\":[null]}")]
    [InlineData("{\"schemaVersion\":1,\"units\":\"mm\",\"footprints\":[{\"contacts\":null}]}")]
    public void InvalidFootprintsProduceDiagnostics(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var result = JsonFootprintRepository.Read(stream);
        Assert.False(result.Success); Assert.Null(result.Value); Assert.NotEmpty(result.Error!);
    }
}
