using System.Collections.Immutable;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Views;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Netlist;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Infrastructure.Repositories;
using CircuitEditor.Simulation.Engine;
using CircuitEditor.Simulation.Scenarios;

namespace CircuitEditor.Tests;

internal static class SimulationPresentationChecks
{
    internal static void Run(ResourceDictionary resources)
    {
        Application.Current.Resources.MergedDictionaries.Add(resources);
        try
        {
            using var design = new ElectricalTestCircuit();
            var electrical = new JsonElectricalLibraryRepository();
            using var playback = new SimulationPlayback(new SimulationSessionFactory(new NetlistBuilder(electrical),
                new DcSolver(electrical), new JsonSimulationModelRepository(), new ScenarioRunner(new JsonScenarioRepository()), []));
            using var simulation = new SimulationViewModel(design.Project, playback, new SimulationUiPulse(), new JsonScenarioRepository());
            simulation.Instruments.Scope.IsVisible = true;
            simulation.Instruments.Scope.Signal = new(History: [new(100, [0d, null], []), new(200, [4.8, null], [])]);
            var deck = new InstrumentDeckView { DataContext = simulation.Instruments };
            var panel = new SimulationInspectorView { DataContext = simulation };
            foreach (var view in new FrameworkElement[] { panel, deck })
            {
                view.Measure(new Size(620, 420));
                view.Arrange(new Rect(0, 0, 620, 420));
                view.UpdateLayout();
                var bitmap = new RenderTargetBitmap(620, 420, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(view);
                Assert.Equal(620, bitmap.PixelWidth);
            }
            simulation.Instruments.Scope.MoveCommand.Execute(new InstrumentMove(10, 20, 200, 240));
            Assert.Equal(40, simulation.Instruments.Scope.Left);
            simulation.Instruments.Scope.SelectProbeCommand.Execute(null);
            var board = design.Project.Model.Breadboards.Single();
            var hole = board.Definition.Holes[0];
            var world = CircuitEditor.Core.Breadboards.BreadboardGeometry.World(board, hole.Position);
            Assert.True(simulation.Instruments.TryPick(world));
            Assert.Contains(hole.Id, simulation.Instruments.Scope.ProbeLabel);
        }
        finally { Application.Current.Resources.MergedDictionaries.Remove(resources); }
    }
}
