using CircuitEditor.Simulation.Engine;
using CircuitEditor.Simulation.Timing;

namespace CircuitEditor.Tests;

public sealed class SimulationClockTests
{
    [Fact]
    public void SameTimeEventsPreserveInsertionOrderAcrossTwoRuns()
    {
        static string[] Run()
        {
            var clock = new SimulationClock();
            var engine = new SimulationEngine(clock);
            var trace = new List<string>();
            engine.Schedule(500, () =>
            {
                trace.Add($"A:{clock.Microseconds}");
                engine.Schedule(500, () => trace.Add($"C:{clock.Microseconds}"));
            });
            engine.Schedule(500, () => trace.Add($"B:{clock.Microseconds}"));
            engine.Schedule(10, () => trace.Add($"D:{clock.Microseconds}"));
            Assert.Equal(1_000, engine.Step().Microseconds);
            return trace.ToArray();
        }

        Assert.Equal(new[] { "D:10", "A:500", "B:500", "C:500" }, Run());
        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void SameTimeFeedbackHaltsAndResetDiscardsPendingEvents()
    {
        var engine = new SimulationEngine(new SimulationClock(), 4);
        void Again() => engine.Schedule(0, Again);
        Again();

        var stopped = engine.Step();
        Assert.True(stopped.IsHalted);
        Assert.Equal(0, stopped.Microseconds);
        Assert.Same(stopped, engine.Step());

        engine.Reset();
        Assert.False(engine.Step().IsHalted);
        Assert.Equal(1_000, engine.State.Microseconds);
    }
}
