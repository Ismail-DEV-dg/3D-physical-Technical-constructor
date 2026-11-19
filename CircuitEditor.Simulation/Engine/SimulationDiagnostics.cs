using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Gpio;

namespace CircuitEditor.Simulation.Engine;

public sealed class SimulationDiagnostics : ISimulationDiagnostics
{
    private const double MilliampsPerAmp = 1000;

    public IEnumerable<SimulationDiagnostic> Read(SimulationCircuit circuit, SimulationModelCatalog catalog, long now)
    {
        if (circuit.PwmApproximation)
            yield return new("SIM_PWM_APPROXIMATION", "Слишком много независимых фаз ШИМ. DC использует приближение для совмещённых фаз.", now);
        foreach (var net in circuit.Nets.Nets)
        {
            var outputs = net.Pins.Where(p => circuit.Controllers.GetValueOrDefault(p.ComponentId)?.Get(p.PinId).Role == PinRole.Output).ToArray();
            if (outputs.Length < 2 || !circuit.PhaseTimes.Any(t => outputs.Select(p =>
                circuit.Controllers[p.ComponentId].OutputAt(p.PinId, t)).Distinct().Count() > 1)) continue;
            var names = outputs.Select(p => $"{circuit.Controllers[p.ComponentId].Reference}.{p.PinId}");
            yield return new("SIM_OUTPUT_CONFLICT", $"{net.Name}: источники на одном узле задают разные логические уровни ({string.Join(", ", names)}).",
                now, outputs[0].ComponentId, outputs[0].PinId, IsError: true);
        }

        var peakBranches = circuit.PhaseResults.SelectMany(r => r.Branches).GroupBy(b => b.Name);
        var components = circuit.Project.Circuit.Components.ToDictionary(c => c.Id);
        foreach (var group in peakBranches)
        {
            var branch = group.First();
            var component = components[branch.ComponentId];
            var amps = group.Max(b => Math.Abs(b.Current ?? 0));
            if (branch.PinA is { } pin && circuit.Controllers.TryGetValue(component.Id, out var board) &&
                SimulationCircuit.GpioBranchName(board, pin) == branch.Name && amps > board.Profile.RecommendedCurrentAmps)
            {
                var critical = amps > board.Profile.AbsoluteCurrentAmps;
                var limit = critical ? board.Profile.AbsoluteCurrentAmps : board.Profile.RecommendedCurrentAmps;
                yield return new(critical ? "SIM_GPIO_ABSOLUTE" : "SIM_GPIO_CURRENT",
                    $"{component.Reference}: ток {pin} {amps * MilliampsPerAmp:0.##} мА при пределе {limit * MilliampsPerAmp:0.##} мА.",
                    now, component.Id, pin, amps, limit, critical);
            }
            if (branch.Limit > 0 && amps > branch.Limit)
                yield return new("SIM_LED_CURRENT",
                    $"{branch.Name}: ток {amps * MilliampsPerAmp:0.##} мА при пределе {branch.Limit * MilliampsPerAmp:0.##} мА.",
                    now, component.Id, branch.PinA, amps, branch.Limit);
            var power = group.Max(b => b.Power ?? 0);
            if (component.Definition.Id == "resistor" &&
                catalog.Components.GetValueOrDefault("resistor")?.GetValueOrDefault("maximumWatts") is double max && power > max)
                yield return new("SIM_RESISTOR_POWER", $"{branch.Name}: мощность {power:0.###} Вт при пределе {max:0.###} Вт.",
                    now, component.Id, Measured: power, Limit: max);
        }
    }

    public SimulationDiagnostic InvalidPin(SimulationCircuit circuit, PinReference pin, GpioState requested, long now)
    {
        var inputOnly = circuit.Controllers.GetValueOrDefault(pin.ComponentId)?.IsInputOnly(pin.PinId) == true;
        return inputOnly && requested.Mode != GpioMode.Input
            ? new("SIM_INPUT_ONLY", $"{pin.PinId}: вывод работает только на вход. Выберите Input без подтяжки.",
                now, pin.ComponentId, pin.PinId)
            : new("SIM_GPIO_MODE", $"{pin.PinId}: режим или значение GPIO недоступны для этого вывода.",
                now, pin.ComponentId, pin.PinId);
    }
}
