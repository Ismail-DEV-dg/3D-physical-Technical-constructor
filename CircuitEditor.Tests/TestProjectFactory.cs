using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Services;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

internal static class TestProjectFactory
{
    public static ProjectViewModel Create(ITextMeasurer? measurer = null) => new(
        new JsonComponentLibraryRepository(), new JsonBreadboardRepository(), new JsonFootprintRepository(),
        new ComponentLayoutCalculator(measurer ?? new TestTextMeasurer()),
        new CircuitEditor.Core.Netlist.NetlistBuilder(new JsonElectricalLibraryRepository()),new JsonElectricalLibraryRepository());
    public static SchematicViewModel Schematic() => new(Create());
    public static BreadboardViewModel Mockup(ProjectViewModel project) => new(project, new ComponentInsertionService());
}
