using CircuitEditor.Core.Models;
using CircuitEditor.Simulation.Gpio;

namespace CircuitEditor.Simulation.Engine;

internal sealed class PwmPhasePlan
{
    private const int MaximumTransitions = 65_536;
    private const int MaximumStates = 128;
    private const long MaximumCycleMicroseconds = 10_000_000;
    private readonly (PinReference Pin, long Period, double Duty)[] outputs;
    private readonly Dictionary<PinReference, int> indices;

    internal PwmPhasePlan(IEnumerable<GpioController> boards)
    {
        outputs = boards.SelectMany(board => board.Pins.Select(pin => (Board: board, Pin: pin.Id, State: board.Get(pin.Id))))
            .Where(p => p.State.Mode == GpioMode.Pwm && p.State.Duty is > 0 and < 1)
            .Select(p => (new PinReference(p.Board.Id, p.Pin), p.Board.Period(p.Pin), p.State.Duty)).ToArray();
        indices = outputs.Select((p, i) => (p.Pin, i)).ToDictionary(p => p.Pin, p => p.i);
        var exact = BuildExact();
        IsApproximation = exact is null;
        Phases = exact ?? BuildApproximation();
    }

    internal IReadOnlyList<PwmPhase> Phases { get; }
    internal bool IsApproximation { get; }
    internal string StateAt(long time) => new(outputs.Select(p =>
        (double)(time % p.Period) / p.Period < p.Duty ? '1' : '0').ToArray());

    internal double PhaseFor(PinReference pin, PwmPhase phase) => indices.TryGetValue(pin, out var index)
        ? phase.State[index] == '1' ? 0 : 1 : 0;

    private IReadOnlyList<PwmPhase>? BuildExact()
    {
        var periods = outputs.Select(p => p.Period).Distinct().ToArray();
        if (periods.Length <= 1) return BuildApproximation();
        var cycle = 1L;
        foreach (var period in periods)
        {
            cycle = cycle / Gcd(cycle, period) * period;
            if (cycle > MaximumCycleMicroseconds) return null;
        }

        var edges = new SortedSet<long> { 0, cycle };
        foreach (var output in outputs)
        {
            var high = (long)Math.Ceiling(output.Duty * output.Period);
            if (edges.Count + 2 * cycle / output.Period > MaximumTransitions) return null;
            for (var start = 0L; start < cycle; start += output.Period)
            {
                edges.Add(start);
                if (high < output.Period) edges.Add(start + high);
            }
        }
        var totals = new Dictionary<string, (long Start, long Duration)>();
        var times = edges.ToArray();
        for (var i = 0; i < times.Length - 1; i++)
        {
            var state = StateAt(times[i]);
            var duration = times[i + 1] - times[i];
            if (!totals.TryGetValue(state, out var total)) total = (times[i], 0);
            totals[state] = (total.Start, total.Duration + duration);
            if (totals.Count > MaximumStates) return null;
        }
        return totals.Select(p => new PwmPhase(p.Key, p.Value.Start, 0, (double)p.Value.Duration / cycle)).ToArray();
    }

    private IReadOnlyList<PwmPhase> BuildApproximation()
    {
        var marks = outputs.Select(p => p.Duty).Append(0).Append(1).Distinct().Order().ToArray();
        return marks[..^1].Select((start, index) =>
            new PwmPhase(new(outputs.Select(p => start < p.Duty ? '1' : '0').ToArray()),
                outputs.Length == 0 ? 0 : (long)Math.Ceiling(start * outputs[0].Period), start, marks[index + 1] - start)).ToArray();
    }

    private static long Gcd(long a, long b)
    {
        while (b != 0) (a, b) = (b, a % b);
        return a;
    }
}
