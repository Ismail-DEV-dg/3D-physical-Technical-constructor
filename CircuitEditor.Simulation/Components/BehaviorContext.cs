using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Engine;
using CircuitEditor.Simulation.Gpio;

namespace CircuitEditor.Simulation.Components;

public sealed class BehaviorContext(SimulationCircuit circuit, SimulationModelCatalog catalog,
    Action<long, Action> schedule)
{
    internal Dictionary<Guid, ComponentMemory> Memory { get; } = [];
    internal Dictionary<Guid, ChipRuntime> Chips { get; } = [];
    internal ChipDefinition? Chip(Component component) => circuit.Project.FindChip(component.Definition.Id);

    internal double? ChipVoltage(Component component, string a, string b)
    {
        var left = circuit.Nets.Find(component.Id, a)?.Id;
        var right = circuit.Nets.Find(component.Id, b)?.Id;
        var result = circuit.ResultAt(Now);
        return left is not null && right is not null
            && result?.References.TryGetValue(left, out var reference) == true
            && result.References.GetValueOrDefault(right) == reference
            ? result.Voltages.GetValueOrDefault(left) - result.Voltages.GetValueOrDefault(right) : null;
    }

    internal ChipLevel ReadChip(Component component, string pin, string ground, LogicFamilyProfile family, double supply)
    {
        var net = circuit.Nets.Find(component.Id, pin);
        var drivers = net is null ? [] : circuit.DriverLevels(net, Now).Distinct().ToArray();
        return drivers.Length > 1 || drivers.Contains(DigitalLevel.X) ? ChipLevel.X
            : family.Read(ChipVoltage(component, pin, ground), supply);
    }

    internal void PublishChip(Component component, IReadOnlyDictionary<int, ChipLevel> outputs)
    {
        foreach (var (pin, level) in outputs)
            circuit.SetChipLevel(new(component.Id, ChipPackage.PinId(pin)), level);
    }

    internal void ScheduleChip(long due, Func<bool> update) => schedule(due, () =>
    {
        if (update()) circuit.InvalidateParameters();
    });

    internal IEnumerable<DcBranchResult> PhaseCurrents(Component c) =>
        circuit.PhaseResults.SelectMany(result => result.Branches.Where(b => b.ComponentId == c.Id));


    public long Now { get; internal set; }

    internal ComponentMemory For(Component c)
    {
        if (!Memory.TryGetValue(c.Id, out var memory)) Memory.Add(c.Id, memory = new());
        return memory;
    }

    public double Parameter(Component c, string key) => catalog.Components[c.Definition.Id][key];
    public double? Voltage(Component c, string a, string b) => circuit.Voltage(new(c.Id, a)) - circuit.Voltage(new(c.Id, b));
    public DigitalLevel Level(Component c, string pin) => circuit.ReadDigital(new(c.Id, pin), Now);
    public double? PulseWidth(Component c, string pin) => circuit.PulseWidth(new(c.Id, pin));
    public bool Powered(Component c, string supply = "VCC", string ground = "GND") =>
        Voltage(c, supply, ground) >= Parameter(c, "minimumVolts");

    public IEnumerable<DcBranch> Branch(Component c, string name, string a, string b, string kind, double value, double vf = 0)
    {
        var from = circuit.Nets.Find(c.Id, a);
        var to = circuit.Nets.Find(c.Id, b);
        if (from is not null && to is not null)
            yield return new(c.Id, $"{c.Reference}.{name}", from.Id, to.Id, kind, value, vf, PinA: a, PinB: b);
    }

    public IEnumerable<DcBranchResult> Currents(Component c) =>
        circuit.Average?.Branches.Where(b => b.ComponentId == c.Id) ?? [];

    public void ChangeAt(long due, Action change) => schedule(due, () =>
    {
        change();
        circuit.InvalidateParameters();
    });

    internal void SetText(Guid id, string text)
    {
        if (!Memory.TryGetValue(id, out var memory)) Memory.Add(id, memory = new());
        memory.Text = text;
    }
}
