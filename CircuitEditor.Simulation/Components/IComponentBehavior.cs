using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Simulation.Components;

public interface IComponentBehavior
{
    string ComponentType { get; }
    IEnumerable<DcBranch> Branches(BehaviorContext context, Component component) => [];
    bool Update(BehaviorContext context, Component component) => false;
    IEnumerable<SimulationDiagnostic> Diagnostics(BehaviorContext context, Component component) => [];
    ComponentSignal? Observe(BehaviorContext context, Component component) => null;
}
