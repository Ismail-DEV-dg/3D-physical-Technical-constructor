using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Tests;

public sealed class SimulationLifecycleTests
{
    [Fact]
    public void SimulationUpdateLeavesHistoryCleanAndProjectChangesInvalidateSnapshot()
    {
        using var project = TestProjectFactory.Create();
        var state = new SimulationState(1_000);
        project.Model.SetSimulation(state);
        Assert.False(project.IsDirty);
        Assert.False(project.CanUndo);

        project.Add(project.Library.First());
        Assert.Equal(new SimulationState(), project.Model.Simulation);
        project.Model.SetSimulation(state);
        project.UndoCommand.Execute(null);
        Assert.Equal(new SimulationState(), project.Model.Simulation);
        project.Model.SetSimulation(state);
        project.RedoCommand.Execute(null);
        Assert.Equal(new SimulationState(), project.Model.Simulation);

        project.Model.SetSimulation(state);
        project.ReplaceProject(new CircuitProject(footprints: project.Model.Footprints));
        Assert.Equal(new SimulationState(), project.Model.Simulation);
        Assert.False(project.IsDirty);
    }
}
