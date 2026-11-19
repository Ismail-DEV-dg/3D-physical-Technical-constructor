using CircuitEditor.Core.Chips;
using CircuitEditor.Simulation.Engine;
using CircuitEditor.Simulation.Timing;

namespace CircuitEditor.Simulation.Instruments;

public sealed class ChipTestBenchFactory(IChipEvaluator evaluator) : IChipTestBenchFactory
{
    public ChipTestBench Create(ChipDefinition definition) => new(definition, evaluator, new SimulationEngine(new SimulationClock()));
}
