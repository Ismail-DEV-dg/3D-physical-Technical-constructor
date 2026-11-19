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
