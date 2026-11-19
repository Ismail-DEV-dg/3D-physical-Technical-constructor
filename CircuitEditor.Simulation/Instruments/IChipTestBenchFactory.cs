using CircuitEditor.Core.Chips;

namespace CircuitEditor.Simulation.Instruments;

public interface IChipTestBenchFactory
{
    ChipTestBench Create(ChipDefinition definition);
}
