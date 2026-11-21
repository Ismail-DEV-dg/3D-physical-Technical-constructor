using CircuitEditor.App.Services;

namespace CircuitEditor.Tests;

internal sealed class TestSimulationPulse : ISimulationUiPulse
{
    public event EventHandler? Tick;
    public void TickOnce() => Tick?.Invoke(this, EventArgs.Empty);
    public void Start() { }
    public void Stop() { }
}
