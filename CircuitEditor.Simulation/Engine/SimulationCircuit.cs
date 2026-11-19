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
    private bool dirty = true;
    private IReadOnlyList<(double Start, DcResult Result)> phases = [];

    public SimulationCircuit(CircuitProject source, INetlistBuilder netlistBuilder, IDcSolver solver, SimulationModelCatalog catalog)
    {
        Project = new ProjectSnapshot(source).Restore();
        this.netlistBuilder = netlistBuilder;
        this.solver = solver;
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
    public bool IterationLimitReached => phases.Any(p => p.Result.IterationLimitReached);

    public bool TrySetPin(PinReference pin, GpioState state)
    {
        if (!controllers.TryGetValue(pin.ComponentId, out var board)) return false;
        var previous = board.Get(pin.PinId);
        if (!board.TrySet(pin.PinId, state)) return false;
        if (previous.Mode != state.Mode || previous.DriveSetting() != state.DriveSetting()) dirty = true;
        return true;
    }

    public void InvalidateParameters() => dirty = true;

    public void RebuildTopology()
    {
        Nets = netlistBuilder.Build(Project);
        matrices.Clear();
        dirty = true;
    }

    public void Refresh(Func<IReadOnlyList<DcBranch>>? componentBranches = null, IReadOnlySet<Guid>? modeled = null)
    {
        if (!dirty) return;
        var bounds = controllers.Values.SelectMany(b => b.Pins.Select(p => b.Get(p.Id)))
            .Where(p => p.Mode == GpioMode.Pwm).Select(p => p.Duty).Append(0).Append(1).Distinct().Order().ToArray();
        var extra = componentBranches?.Invoke() ?? [];
        var segments = new List<(double Start, DcResult Result)>();
        for (var i = 0; i < bounds.Length - 1; i++)
        {
            segments.Add((bounds[i], solver.Solve(Project, Nets, Drivers(bounds[i]).Concat(extra).ToArray(), matrices, modeled)));
            SolveCount++;
        }

        phases = segments;
        Average = PwmAveraging.Combine(segments);
        dirty = false;
    }

    public double? Voltage(PinReference pin) => Nets.Find(pin.ComponentId, pin.PinId) is { } net
        ? Average?.Voltages.GetValueOrDefault(net.Id) : null;

    public DigitalLevel ReadDigital(PinReference pin, long microseconds)
    {
        var net = Nets.Find(pin.ComponentId, pin.PinId);
        if (net is null) return DigitalLevel.Z;
        var active = net.Pins.Where(p => controllers.ContainsKey(p.ComponentId)).Select(p =>
            controllers[p.ComponentId].OutputAt(p.PinId, microseconds)).Where(level => level != DigitalLevel.Z).Distinct().ToArray();
        if (active.Length > 1) return DigitalLevel.X;
        var reference = controllers.GetValueOrDefault(pin.ComponentId) ?? net.Pins
            .Select(p => controllers.GetValueOrDefault(p.ComponentId)).FirstOrDefault(b => b is not null);
        // Directly driven nets use the driver's instantaneous PWM phase.
        var index = reference is null ? 0 : (double)(microseconds % reference.Profile.PwmPeriodMicroseconds) / reference.Profile.PwmPeriodMicroseconds;
        var result = phases.LastOrDefault(p => p.Start <= index).Result;
        var voltage = result?.Voltages.GetValueOrDefault(net.Id);
        if (voltage is null) return active.Length == 1 ? active[0] : DigitalLevel.Z;
        var supply = reference?.Profile.LogicVoltage ?? controllers.Values.FirstOrDefault()?.Profile.LogicVoltage;
        if (supply is null) return DigitalLevel.X;
        return voltage <= supply * LowThresholdFraction ? DigitalLevel.Low :
            voltage >= supply * HighThresholdFraction ? DigitalLevel.High : DigitalLevel.X;
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

    private IEnumerable<DcBranch> Drivers(double phase)
    {
        foreach (var board in controllers.Values)
        {
            var component = Project.Circuit.Components.Single(c => c.Id == board.Id);
            var ground = component.Definition.Pins.Where(p => p.Type == PinType.Ground)
                .Select(p => Nets.Find(board.Id, p.Id)).FirstOrDefault(n => n is not null);
            if (ground is null) continue;
            foreach (var pin in board.Pins)
            {
                if (board.Drive(pin.Id, phase) is not double voltage || Nets.Find(board.Id, pin.Id) is not { } net) continue;
                var resistance = board.Get(pin.Id).Mode == GpioMode.InputPullup
                    ? board.Profile.PullupResistanceOhms : board.Profile.OutputResistanceOhms;
                yield return new(board.Id, GpioBranchName(board, pin.Id), net.Id, ground.Id, "TH", resistance, voltage);
            }
        }
    }

    public static string GpioBranchName(GpioController board, string pinId) => $"{board.Reference}.GPIO.{pinId}";
}
