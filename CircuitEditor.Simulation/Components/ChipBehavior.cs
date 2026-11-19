using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Simulation.Components;

public sealed class ChipBehavior(ILogicFamilyRepository repository, IChipEvaluator evaluator) : IComponentBehavior
{
    internal const string TypeId = "chip";
    private const string DriverPrefix = "CHIP.";
    private const double VoltageTolerance = 1e-6;
    private const int MilliampsPerAmp = 1_000;
    public string ComponentType => TypeId;

    private readonly IReadOnlyDictionary<string, LogicFamilyProfile> families =
        repository.Load().Value ?? new Dictionary<string, LogicFamilyProfile>();

    public bool Update(BehaviorContext context, Component component)
    {
        if (context.Chip(component) is not { } definition || !families.TryGetValue(definition.Family, out var family))
            return false;
        if (!context.Chips.TryGetValue(component.Id, out var runtime))
        {
            runtime = new(definition, evaluator)
            {
                GroundPin = ChipPackage.PinId(definition.Pins.First(pin => pin.Type == ChipPinType.Gnd).Number),
                SupplyPin = ChipPackage.PinId(definition.Pins.First(pin => pin.Type == ChipPinType.Vcc).Number)
            };
            context.Chips[component.Id] = runtime;
        }
        var supply = context.ChipVoltage(component, runtime.SupplyPin, runtime.GroundPin);
        var powered = supply >= family.MinimumSupply - VoltageTolerance && supply <= family.MaximumSupply + VoltageTolerance
            && definition.Pins.Where(pin => pin.Type is ChipPinType.Vcc or ChipPinType.Gnd).All(pin =>
                context.ChipVoltage(component, ChipPackage.PinId(pin.Number),
                    pin.Type == ChipPinType.Vcc ? runtime.SupplyPin : runtime.GroundPin) is double diff && Math.Abs(diff) <= VoltageTolerance);
        var nextSupply = powered ? supply!.Value : 0d;
        var changed = Math.Abs(runtime.Supply - nextSupply) > VoltageTolerance;
        runtime.Supply = nextSupply;
        var inputs = powered ? definition.Pins.Where(pin => pin.Reads).ToDictionary(pin => pin.Number,
            pin => context.ReadChip(component, ChipPackage.PinId(pin.Number), runtime.GroundPin, family, nextSupply)) : [];
        var updated = runtime.Apply(powered, inputs, context.Now, context.ScheduleChip,
            outputs => context.PublishChip(component, outputs));
        return changed || updated;
    }

    public IEnumerable<DcBranch> Branches(BehaviorContext context, Component component)
    {
        if (!context.Chips.TryGetValue(component.Id, out var runtime) || runtime.Supply <= 0
            || context.Chip(component) is not { } definition || !families.TryGetValue(definition.Family, out var family))
            yield break;
        foreach (var (pin, level) in runtime.Outputs)
        {
            if (level is not (ChipLevel.Low or ChipLevel.High)) continue;
            var high = level == ChipLevel.High;
            var id = ChipPackage.PinId(pin);
            foreach (var branch in context.Branch(component, DriverPrefix + id, id, high ? runtime.SupplyPin : runtime.GroundPin,
                "TH", high ? family.SourceResistanceOhms : family.SinkResistanceOhms,
                high ? family.OutputHigh.Resolve(runtime.Supply) - runtime.Supply : family.OutputLow.Resolve(runtime.Supply)))
                yield return branch;
        }
    }

    public IEnumerable<SimulationDiagnostic> Diagnostics(BehaviorContext context, Component component)
    {
        if (!context.Chips.TryGetValue(component.Id, out var runtime) ||
            context.Chip(component) is not { } definition || !families.TryGetValue(definition.Family, out var family))
            yield break;
        if (runtime.Supply <= 0)
            yield return new("CHIP_POWER", $"{component.Reference}: VCC/GND должны питаться в диапазоне {family.MinimumSupply:0.##}–{family.MaximumSupply:0.##} В.",
                context.Now, component.Id, runtime.SupplyPin);
        if (!runtime.Settled)
            yield return new("CHIP_OSCILLATION", $"{component.Reference}: внутренняя логика не стабилизировалась.",
                context.Now, component.Id, IsError: true);
        foreach (var branch in context.PhaseCurrents(component).Where(branch => branch.PinA is not null
                     && branch.Name.StartsWith(component.Reference + "." + DriverPrefix, StringComparison.Ordinal))
                     .GroupBy(branch => branch.PinA).SelectMany(group => group
                         .Where(branch => branch.Current is double amps && family.ExceedsCurrentLimit(amps))
                         .OrderByDescending(branch => Math.Abs(branch.Current!.Value)).Take(1)))
        {
            var current = branch.Current!.Value;
            var direction = current > 0 ? "втекающий" : "вытекающий";
            var limit = current > 0 ? family.SinkLimitAmps : family.SourceLimitAmps;
            yield return new("CHIP_OUTPUT_CURRENT",
                $"{component.Reference}.{branch.PinA}: {direction} ток {Math.Abs(current) * MilliampsPerAmp:0.##} мА, предел {limit * MilliampsPerAmp:0.##} мА.",
                context.Now, component.Id, branch.PinA);
        }
    }
}
