using System.IO;
using System.Text.Json;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Code;
using CircuitEditor.Core.Documents;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Services;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Core.Wiring;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

public sealed class ProjectChipStorageTests
{
    [Fact]
    public void ActualV3ShapeMigratesWithoutChangingSketchConnectionsAndSimulationSetup()
    {
        using var test = new Stage4TestEnvironment();
        var board = test.Project.Add(test.Project.Library.Single(part => part.Id == "arduino-uno"))!.Model;
        var sketch = new SketchDocument("void setup() {}", BoardKind.Uno, "Ревизия_3");
        test.Project.UpdateSketch(sketch);
        var scenario = new ScenarioConfiguration("blink",
            System.Collections.Immutable.ImmutableDictionary<string, PinReference>.Empty.Add("LED_PIN", new(board.Id, "D13")),
            System.Collections.Immutable.ImmutableDictionary<string, double>.Empty.Add("periodMs", 100));
        test.Project.UpdateSimulationSetup(new SimulationSetup { Scenario = scenario });
        var original = test.Session.Capture();
        var old = JsonSerializer.SerializeToNode(original, JsonProjectRepository.Options)!.AsObject();
        old["schemaVersion"] = 3;
        old.Remove("embeddedChips");
        var path = Path.Combine(test.Folder, "v3.circ");
        File.WriteAllText(path, old.ToJsonString());
        var migrated = test.Repository.Load(path);
        Assert.True(migrated.Success, migrated.Error);
        Assert.Equal(4, migrated.Value!.SchemaVersion);
        Assert.Empty(migrated.Value.EmbeddedChips);
        Assert.Equal(original.Sketch, migrated.Value.Sketch);
        Assert.Equal(original.Components, migrated.Value.Components);
        Assert.True(original.SimulationSetup.Matches(migrated.Value.SimulationSetup));
        Assert.True(test.Session.Load(path));
        Assert.False(test.Project.IsDirty);
    }

    [Fact]
    public void EmbeddedDefinitionsDeduplicateAndOpenWithoutLocalLibrary()
    {
        using var test = new Stage4TestEnvironment();
        var chip = ChipTemplateTests.Get("7400");
        var original = ProjectWithChip(chip, out var componentId);
        original.AddComponent(ChipPackage.Component(chip, ChipTruthTableTests.Families[chip.Family]));
        var secondVersion = chip with { DefinitionVersion = 2 };
        original.RegisterChip(secondVersion);
        original.AddComponent(ChipPackage.Component(secondVersion, ChipTruthTableTests.Families[chip.Family]));
        var document = ProjectDocument.Capture(original);
        Assert.Equal(2, document.EmbeddedChips.Count);
        Assert.Equal(3, document.Components.Count(part => part.ChipId == chip.Id));
        var path = Path.Combine(test.Folder, "portable.circ");
        Assert.Null(test.Repository.Save(path, document));
        var json = File.ReadAllText(path);
        Assert.Contains("embeddedChips", json);
        Assert.DoesNotContain("componentDefinitionId", json);
        Assert.DoesNotContain("holePitch", json);

        var profiles = new JsonLogicFamilyRepository();
        var storage = new JsonChipRepository(profiles, [], Path.Combine(test.Folder, "empty-library"));
        using var session = new ProjectSessionViewModel(test.Project, test.Mockup, test.Schematic, test.Navigation, test.Repository,
            test, test, new TestProjectAutosave(), new JsonBreadboardRepository(), new ComponentInsertionService(),
            profiles, new ProjectChipVersionSelector(new UserChipLibraryService(storage, profiles), test));
        Assert.True(session.Load(path));
        Assert.Equal(2, test.Project.Model.Chips.Count);
        Assert.Contains(test.Project.Components, part => part.Model.Id == componentId && part.Footprint.Renderer == "dip-chip");
        Assert.Equal(original.Bindings.ToArray(), test.Project.Model.Bindings.ToArray());
        Assert.Equal(original.Circuit.Wires.Select(wire => wire.Id), test.Project.Model.Circuit.Wires.Select(wire => wire.Id));
        Assert.Equal(0, test.ChipVersionQuestions);
    }

    [Theory]
    [InlineData(false, false, false, 1)]
    [InlineData(true, false, false, 2)]
    [InlineData(true, true, false, 1)]
    [InlineData(true, false, true, 1)]
    public void ConflictAllowsEmbeddedOrCompatibleUpgradeAndKeepsProjectOnIncompatibleVersion(
        bool useLocal, bool incompatible, bool sameRevision, int expectedRevision)
    {
        using var test = new Stage4TestEnvironment();
        var profiles = new JsonLogicFamilyRepository();
        var storage = new JsonChipRepository(profiles, [], Path.Combine(test.Folder, "chips"));
        var old = ChipTemplateTests.Get("7400");
        var replacement = incompatible
            ? ChipTruthTableTests.Nand() with { Id = old.Id, DefinitionVersion = 2, Name = "DIP-8" }
            : old with { DefinitionVersion = sameRevision ? 1 : 2, Name = "NAND обновлён" };
        Assert.Null(storage.Store(replacement));
        test.UseLibraryChip = useLocal;
        using var session = new ProjectSessionViewModel(test.Project, test.Mockup, test.Schematic, test.Navigation, test.Repository,
            test, test, new TestProjectAutosave(), new JsonBreadboardRepository(), new ComponentInsertionService(),
            profiles, new ProjectChipVersionSelector(new UserChipLibraryService(storage, profiles), test));
        var path = Path.Combine(test.Folder, "conflicts.circ");
        Assert.Null(test.Repository.Save(path, ProjectDocument.Capture(ProjectWithChip(old, out var id))));
        Assert.True(session.Load(path));
        var chip = Assert.Single(test.Project.Model.Chips);
        Assert.Equal(1, test.ChipVersionQuestions);
        Assert.Equal(expectedRevision, chip.DefinitionVersion);
        Assert.Equal(useLocal && !incompatible ? replacement.Name : old.Name, chip.Name);
        Assert.Equal(useLocal && !incompatible, test.Project.IsDirty);
        Assert.Equal(incompatible ? 1 : 0, test.Warnings.Count);
        Assert.Contains(test.Project.Model.Bindings, binding => binding.ComponentId == id && binding.HoleId == "left/E/02");
    }

    [Fact]
    public void MissingEmbeddedDefinitionDoesNotReplaceOpenProject()
    {
        using var test = new Stage4TestEnvironment();
        var old = test.Project.Add(test.Project.Library[0])!.Model;
        var invalid = ProjectDocument.Capture(ProjectWithChip(ChipTemplateTests.Get("7400"), out _)) with { EmbeddedChips = [] };
        var path = Path.Combine(test.Folder, "bad.circ");
        Assert.Null(test.Repository.Save(path, invalid));
        Assert.False(test.Session.Load(path));
        Assert.Equal(old.Id, test.Project.Components.Single().Model.Id);
        Assert.Contains("встроенную микросхему", Assert.Single(test.Errors));
    }

    private static CircuitProject ProjectWithChip(ChipDefinition chip, out Guid componentId)
    {
        var project = BreadboardTestData.Create();
        project.RegisterChip(chip);
        var component = project.AddComponent(ChipPackage.Component(chip, ChipTruthTableTests.Families[chip.Family]));
        componentId = component.Id;
        var board = project.Breadboards[0];
        var hole = board.Definition.Holes.Single(item => item.Id == "left/E/02");
        var inserted = new ComponentInsertionService().Insert(project, component.Id, ChipPackage.Footprint(chip),
            board.Id, new ComponentPlacement(board.X + hole.Position.X, board.Y + hole.Position.Y));
        Assert.True(inserted.Success, inserted.Error);
        var uno = BreadboardTestData.Add(project, "arduino-uno");
        var groundContact = BreadboardTestData.Footprint(project, "arduino-uno").Contacts.First(contact => contact.ElectricalPinId == "GND");
        project.Circuit.PutWire(new Wire(WireEndpoint.Contact(uno.Id, groundContact.Id), WireEndpoint.Contact(component.Id, "7")));
        return project;
    }
}
