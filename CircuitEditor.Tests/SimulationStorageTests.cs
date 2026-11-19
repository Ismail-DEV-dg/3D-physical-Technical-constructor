using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Code;
using CircuitEditor.Core.Documents;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Netlist;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Infrastructure.Repositories;
using CircuitEditor.Simulation.Engine;
using CircuitEditor.Simulation.Scenarios;

namespace CircuitEditor.Tests;

public sealed class SimulationStorageTests
{
    [Fact]
    public void VersionTwoFileMigratesWithSketchAndGeometryIntact()
    {
        using var test = new Stage4TestEnvironment();
        test.Project.Add(test.Project.Library.First(c => c.Id == "arduino-uno"));
        test.Project.UpdateSketch(new("void setup() {}\nvoid loop() {}", BoardKind.Uno, "Legacy"));
        var document = test.Session.Capture();
        var node = JsonSerializer.SerializeToNode(document, JsonProjectRepository.Options)!.AsObject();
        node["schemaVersion"] = 2;
        node.Remove("simulationSetup");
        node.Remove("embeddedChips");
        var path = Path.Combine(test.Folder, "version-two.circ");
        File.WriteAllText(path, node.ToJsonString());
        var migrated = test.Repository.Load(path);
        Assert.True(migrated.Success, migrated.Error);
        Assert.Equal(CircuitEditor.Core.Documents.ProjectDocument.CurrentSchemaVersion, migrated.Value!.SchemaVersion);
        Assert.Equal(document.Sketch, migrated.Value.Sketch);
        Assert.Equal(document.Components, migrated.Value.Components);
        Assert.Equal(document.Breadboards, migrated.Value.Breadboards);
        Assert.True(new SimulationSetup().Matches(migrated.Value.SimulationSetup));
        Assert.True(test.Session.Load(path));
        Assert.False(test.Project.IsDirty);
    }

    [Fact]
    public void ConfigurationRoundTripsButHistoryAndRunningSignalsDoNot()
    {
        using var test = new Stage4TestEnvironment();
        var board = test.Project.Add(test.Project.Library.First(c => c.Id == "arduino-uno"))!;
        test.Project.MarkSaved();
        var electrical = new JsonElectricalLibraryRepository();
        using var playback = new SimulationPlayback(new SimulationSessionFactory(new NetlistBuilder(electrical), new DcSolver(electrical),
            new JsonSimulationModelRepository(), new ScenarioRunner(new JsonScenarioRepository()), []));
        using var simulation = new SimulationViewModel(test.Project, playback, new TestSimulationPulse(), new JsonScenarioRepository());
        simulation.Scenario.Template = simulation.Scenario.Templates.Single(t => t.Id == "blink");
        var binding = simulation.Scenario.Bindings.Single();
        binding.Selected = binding.Options.Single(p => p.Reference == new PinReference(board.Model.Id, "D13"));
        simulation.Scenario.Numbers.Single(n => n.Name == "periodMs").Value = 200;
        simulation.Instruments.Scope.IsVisible = true;
        simulation.Instruments.Scope.MoveCommand.Execute(new InstrumentMove(20, 10, 300, 200));
        var configured = test.Project.Model.SimulationSetup;
        Assert.True(test.Project.IsDirty);
        Assert.Equal(1, test.Project.History.Count);

        test.Project.Model.SetSimulation(new(50_000)
        {
            Serial = [new(1, 1_000, "временная строка")],
            Instruments = ImmutableDictionary<InstrumentKind, InstrumentSignal>.Empty.Add(InstrumentKind.Scope,
                new(History: [new(1_000, [5d, null], [])]))
        });
        var path = Path.Combine(test.Folder, "version-three.circ");
        test.Target = path;
        test.Session.SaveCommand.Execute(null);
        Assert.False(test.Project.IsDirty);
        var json = File.ReadAllText(path);
        Assert.DoesNotContain("microseconds", json);
        Assert.DoesNotContain("временная строка", json);
        Assert.DoesNotContain("history", json);
        Assert.Contains("simulationSetup", json);
        Assert.True(test.Session.Load(path));
        Assert.True(configured.Matches(test.Project.Model.SimulationSetup));
        Assert.Equal(50, simulation.Instruments.Scope.Left);
        Assert.True(simulation.Instruments.Scope.IsVisible);
        Assert.Equal(200, simulation.Scenario.Numbers.Single(n => n.Name == "periodMs").Value);
        Assert.Equal(0, test.Project.Model.Simulation.Microseconds);
        Assert.Empty(test.Project.Model.Simulation.Serial);
        Assert.Empty(test.Project.Model.Simulation.Instruments);
        Assert.False(test.Project.IsDirty);
    }

    [Fact]
    public void RunningSessionGuardsEditsWithoutChangingDirtyFlagAndFileOpenStopsIt()
    {
        using var test = new Stage4TestEnvironment();
        var button = test.Project.Add(test.Project.Library.First(c => c.Id == "button"))!;
        test.Target = Path.Combine(test.Folder, "saved.circ");
        test.Session.SaveCommand.Execute(null);
        var electrical = new JsonElectricalLibraryRepository();
        using var playback = new SimulationPlayback(new SimulationSessionFactory(new NetlistBuilder(electrical), new DcSolver(electrical),
            new JsonSimulationModelRepository(), new ScenarioRunner(new JsonScenarioRepository()), []));
        using var simulation = new SimulationViewModel(test.Project, playback, new TestSimulationPulse(), new JsonScenarioRepository());
        simulation.StepCommand.Execute(null);
        Assert.True(simulation.HasSession);
        Assert.False(test.Project.CanEdit);
        Assert.False(test.Project.IsDirty);
        Assert.Null(test.Project.Add(test.Project.Library[0]));
        test.Project.RemoveSelected();
        Assert.Single(test.Project.Components);
        simulation.Press(button.Model.Id, true);
        Assert.Equal(0, button.Model.Inputs.Values[ComponentInput.ButtonPressed]);
        Assert.Equal(1, test.Project.History.Count);

        Assert.True(test.Session.Load(test.Target));
        Assert.False(simulation.HasSession);
        Assert.True(test.Project.CanEdit);
        Assert.False(test.Project.IsDirty);
        Assert.Equal(0, test.Project.History.Count);
    }
}
