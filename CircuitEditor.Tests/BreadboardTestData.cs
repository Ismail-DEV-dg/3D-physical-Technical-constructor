using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Physical;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

internal static class BreadboardTestData
{
    public static CircuitProject Create()
    {
        var board = new JsonBreadboardRepository().Load();
        var footprints = new JsonFootprintRepository().Load();
        Assert.True(board.Success, board.Error); Assert.True(footprints.Success, footprints.Error);
        return new CircuitProject(breadboards: [new BreadboardInstance(Guid.NewGuid(), board.Value!, 90, 5)], footprints: footprints.Value);
    }
    public static Component Add(CircuitProject project, string id = "resistor") =>
        project.AddComponent(new JsonComponentLibraryRepository().Load().Components.Single(item => item.Id == id));
    public static FootprintDefinition Footprint(CircuitProject project, string id = "resistor") =>
        project.Footprints.Single(item => item.ComponentDefinitionId == id);
}
