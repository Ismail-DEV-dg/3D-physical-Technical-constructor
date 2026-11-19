using System.Text.Json.Nodes;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

public sealed class ChipLibraryTests
{
    [Fact]
    public void LibraryRefreshesPaletteWithoutRestartAndProjectOwnsPlacedVersion()
    {
        using var temporary = new Stage4TestEnvironment();
        var families = new JsonLogicFamilyRepository();
        var storage = new JsonChipRepository(families, [], temporary.Folder);
        var library = new UserChipLibraryService(storage, families);
        using var project = new ProjectViewModel(new JsonComponentLibraryRepository(), new JsonBreadboardRepository(),
            new JsonFootprintRepository(), new ComponentLayoutCalculator(new TestTextMeasurer()), chipLibrary: library);
        var editor = new ChipViewModel(families, library: library, storage: storage,
            templates: new JsonChipTemplateRepository(families));
        editor.Library.SelectedTemplate = editor.Library.Templates.Single(item => item.Name == "7400");
        editor.Library.UseTemplateCommand.Execute(null);
        Assert.Empty(editor.Problems);
        editor.Library.SaveCommand.Execute(null);
        var first = Assert.Single(library.Definitions);
        var palette = project.PaletteLibrary.Single(definition => definition.Id == first.ComponentId);
        Assert.Equal("Микросхемы", palette.Category);
        var component = project.Add(palette);
        Assert.NotNull(component);
        Assert.Equal("dip-chip", component.Footprint.Renderer);
        Assert.Equal(14, component.Layout.Pins.Count);
        Assert.True(project.CanUndo);
        project.UndoCommand.Execute(null);
        Assert.Empty(project.Model.Circuit.Components);
        project.RedoCommand.Execute(null);
        Assert.Equal(first.ComponentId, project.Model.Circuit.Components.Single().Definition.Id);
        editor.ChipName = "Копия 7400";
        editor.ApplyPinsCommand.Execute(null);
        editor.Library.SaveCommand.Execute(null);
        Assert.Equal(2, library.Definitions.Single().DefinitionVersion);
        Assert.Equal(1, project.Model.Chips.Single().DefinitionVersion);
        Assert.Null(storage.Delete(first.Id, 2));
        Assert.Null(library.Refresh());
        Assert.Equal(27, project.PaletteLibrary.Count);
        Assert.Single(project.Model.Chips);
    }

    [Fact]
    public void ChipContainerRoundTripsMigrationsAndRejectsUnknownVersions()
    {
        using var temporary = new Stage4TestEnvironment();
        var profiles = new JsonLogicFamilyRepository();
        var storage = new JsonChipRepository(profiles, [new TestChipMigration()], temporary.Folder);
        var chip = ChipTemplateTests.Get("74HC595");
        var path = System.IO.Path.Combine(temporary.Folder, "portable.chip");
        Assert.Null(storage.Export(path, chip));
        var loaded = storage.Load(path);
        Assert.True(loaded.Success, loaded.Error);
        Assert.True(ChipContent.Matches(chip, loaded.Value!));
        var json = JsonNode.Parse(System.IO.File.ReadAllText(path))!.AsObject();
        var definition = json["definition"]!.DeepClone();
        json.Remove("definition");
        json["legacyChip"] = definition;
        json["schemaVersion"] = 0;
        System.IO.File.WriteAllText(path, json.ToJsonString());
        Assert.True(ChipContent.Matches(chip, storage.Load(path).Value!));
        json["schemaVersion"] = 2;
        System.IO.File.WriteAllText(path, json.ToJsonString());
        var error = storage.Load(path).Error!;
        Assert.Contains(".chip: 2", error);
        Assert.Contains("версия: 1", error);
    }
}
