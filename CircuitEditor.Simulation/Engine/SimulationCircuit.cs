using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.History;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Netlist;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Gpio;

namespace CircuitEditor.Simulation.Engine;

public sealed class SimulationCircuit
{
    private const double LowThresholdFraction = 0.3, HighThresholdFraction = 0.7;
    private readonly INetlistBuilder netlistBuilder;
    private readonly IDcSolver solver;
    private readonly DcMatrixCache matrices = new();
    private readonly Dictionary<Guid, GpioController> controllers;
    private readonly WireMeasurements wireMeasurements;
    private readonly Dictionary<PinReference, DigitalLevel> chipLevels = [];
    private bool dirty = true;
    private PwmPhasePlan? pwm;
    private Dictionary<string, DcResult> phases = [];

    public SimulationCircuit(CircuitProject source, INetlistBuilder netlistBuilder, IDcSolver solver, SimulationModelCatalog catalog)
    {
        Project = new ProjectSnapshot(source).Restore();
        this.netlistBuilder = netlistBuilder;
        this.solver = solver;
        wireMeasurements = new(Project, netlistBuilder);
        controllers = Project.Circuit.Components.Where(c => catalog.Boards.ContainsKey(c.Definition.Id))
            .ToDictionary(c => c.Id, c => new GpioController(c, catalog.Boards[c.Definition.Id]));
        Nets = netlistBuilder.Build(Project);
    }

    public CircuitProject Project { get; }
    public Netlist Nets { get; private set; }
    public IReadOnlyDictionary<Guid, GpioController> Controllers => controllers;
    public DcResult? Average { get; private set; }
    public int SolveCount { get; private set; }
    public int MatrixBuildCount => matrices.BuildCount;
    public bool IsDirty => dirty;
    public bool PwmApproximation => pwm?.IsApproximation == true;
    public IEnumerable<long> PhaseTimes => pwm?.Phases.Select(p => p.At) ?? [];
    public IEnumerable<DcResult> PhaseResults => phases.Values;
    public bool IterationLimitReached => phases.Values.Any(p => p.IterationLimitReached);

    public bool TrySetPin(PinReference pin, GpioState state)
    {
        if (!controllers.TryGetValue(pin.ComponentId, out var board)) return false;
        var previous = board.Get(pin.PinId);
        if (!board.TrySet(pin.PinId, state)) return false;
        if (previous.Mode != state.Mode || previous.DriveSetting() != state.DriveSetting() || state.Mode == GpioMode.Pwm && previous.PeriodMicroseconds != state.PeriodMicroseconds) dirty = true;
        return true;
    }

    public void InvalidateParameters() => dirty = true;

    public void RebuildTopology()
    {
        Nets = netlistBuilder.Build(Project);
        matrices.Clear();
        wireMeasurements.Invalidate();
        dirty = true;
    }

    public void Refresh(Func<IReadOnlyList<DcBranch>>? componentBranches = null, IReadOnlySet<Guid>? modeled = null)
    {
        if (!dirty) return;
        pwm = new(controllers.Values);
        var extra = componentBranches?.Invoke() ?? [];
        var results = new List<(double Weight, DcResult Result)>();
        phases = [];
        foreach (var phase in pwm.Phases)
        {
            var result = solver.Solve(Project, Nets, Drivers(pwm, phase).Concat(extra).ToArray(), matrices, modeled);
            phases.Add(phase.State, result);
            results.Add((phase.Weight, result));
            SolveCount++;
        }
        Average = PwmAveraging.Combine(results);
        dirty = false;
    }

    public System.Collections.Immutable.ImmutableDictionary<Guid, WireSignal> Wires() => wireMeasurements.Read(Nets, Average);
    public DcResult? ResultAt(long microseconds) => pwm is null ? Average : phases.GetValueOrDefault(pwm.StateAt(microseconds)) ?? Average;

    public double? Voltage(PinReference pin) => Nets.Find(pin.ComponentId, pin.PinId) is { } net
        ? Average?.Voltages.GetValueOrDefault(net.Id) : null;

    public DigitalLevel ReadDigital(PinReference pin, long microseconds)
    {
        var net = Nets.Find(pin.ComponentId, pin.PinId);
        return net is null ? DigitalLevel.Z : ReadDigital(net, microseconds, pin);
    }

    public DigitalLevel ReadDigital(Net net, long microseconds, PinReference? pin = null)
    {
        var active = DriverLevels(net, microseconds).Distinct().ToArray();
        if (active.Length > 1 || active.Contains(DigitalLevel.X)) return DigitalLevel.X;
        var reference = (pin is null ? null : controllers.GetValueOrDefault(pin.ComponentId)) ?? net.Pins
            .Select(p => controllers.GetValueOrDefault(p.ComponentId)).FirstOrDefault(b => b is not null);
        var result = ResultAt(microseconds);
        var voltage = result?.Voltages.GetValueOrDefault(net.Id);
        if (voltage is null) return active.Length == 1 ? active[0] : DigitalLevel.Z;
        if (reference is null && active.Length == 1 && net.Pins.Any(chipLevels.ContainsKey)) return active[0];
        var supply = reference?.Profile.LogicVoltage ?? controllers.Values.FirstOrDefault()?.Profile.LogicVoltage;
        if (supply is null) return active.Length == 1 ? active[0] : DigitalLevel.X;
        return voltage <= supply * LowThresholdFraction ? DigitalLevel.Low :
            voltage >= supply * HighThresholdFraction ? DigitalLevel.High : DigitalLevel.X;
    }

    internal IEnumerable<DigitalLevel> DriverLevels(Net net, long micros) =>
        net.Pins.Select(pin => controllers.GetValueOrDefault(pin.ComponentId)?.OutputAt(pin.PinId, micros)
                              ?? chipLevels.GetValueOrDefault(pin, DigitalLevel.Z))
            .Where(level => level != DigitalLevel.Z);

    public DigitalLevel ChipOutput(PinReference pin) => chipLevels.GetValueOrDefault(pin, DigitalLevel.Z);

    internal void SetChipLevel(PinReference pin, ChipLevel level)
    {
        if (level == ChipLevel.Z) { chipLevels.Remove(pin); return; }
        chipLevels[pin] = level switch { ChipLevel.Low => DigitalLevel.Low, ChipLevel.High => DigitalLevel.High, _ => DigitalLevel.X };
    }


    public double? PulseWidth(PinReference input)
    {
        var drivers = Nets.Find(input.ComponentId, input.PinId)?.Pins
            .Where(p => controllers.GetValueOrDefault(p.ComponentId)?.Get(p.PinId).Mode == GpioMode.Pwm).ToArray();
        if (drivers?.Length != 1) return null;
        var driver = drivers[0];
        var board = controllers[driver.ComponentId];
        var duty = board.Get(driver.PinId).Duty;
        return duty is > 0 and < 1 ? duty * board.Period(driver.PinId) : null;
    }

    private IEnumerable<DcBranch> Drivers(PwmPhasePlan plan, PwmPhase phase)
    {
        foreach (var board in controllers.Values)
        {
            var component = Project.Circuit.Components.Single(c => c.Id == board.Id);
            var ground = component.Definition.Pins.Where(p => p.Type == PinType.Ground)
                .Select(p => Nets.Find(board.Id, p.Id)).FirstOrDefault(n => n is not null);
            if (ground is null) continue;
            foreach (var pin in board.Pins)
            {
                if (board.Drive(pin.Id, plan.PhaseFor(new(board.Id, pin.Id), phase)) is not double voltage || Nets.Find(board.Id, pin.Id) is not { } net) continue;
                var resistance = board.Get(pin.Id).Mode == GpioMode.InputPullup
                    ? board.Profile.PullupResistanceOhms : board.Profile.OutputResistanceOhms;
                yield return new(board.Id, GpioBranchName(board, pin.Id), net.Id, ground.Id, "TH", resistance, voltage,
                    PinA: pin.Id, PinB: ground.Pins.First(p => p.ComponentId == board.Id &&
                        component.Definition.Pins.Any(pin => pin.Id == p.PinId && pin.Type == PinType.Ground)).PinId);
            }
        }
    }

    public static string GpioBranchName(GpioController board, string pinId) => $"{board.Reference}.GPIO.{pinId}";
}
