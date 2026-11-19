using CircuitEditor.Core.Chips;

namespace CircuitEditor.Simulation.Instruments;

public static class ChipBenchVerification
{
    public static IReadOnlyList<string> Verify(ChipDefinition chip, ChipTruthTable expected, IChipTestBenchFactory factory)
    {
        var bench = factory.Create(chip);
        List<string> differences = [];
        foreach (var section in expected.Sections)
        {
            foreach (var pattern in ChipTruthLogic.Patterns(section.Inputs.Count))
            {
                var inputs = section.Inputs.Select((pin, index) => (pin, level: pattern[index] == '0' ? ChipLevel.Low : ChipLevel.High))
                    .ToDictionary(item => item.pin, item => item.level);
                foreach (var pin in chip.Pins.Where(pin => section.Outputs.Contains(pin.Number) && pin.EnablePin.HasValue))
                    inputs.TryAdd(pin.EnablePin!.Value, pin.EnableLow ? ChipLevel.Low : ChipLevel.High);
                bench.SetInputs(inputs);
                var reference = ChipTruthLogic.Evaluate(new([section]), inputs);
                foreach (var (pin, level) in reference)
                {
                    var actual = bench.Outputs[pin];
                    if (actual != level) differences.Add($"Входы {pattern}, вывод {pin}: ожидалось {ChipTruthLogic.Symbol(level)}, получено {ChipTruthLogic.Symbol(actual)}.");
                }
            }
        }
        return differences;
    }
}
