using CircuitEditor.App.Services;

namespace CircuitEditor.Tests;

internal sealed class TestSimulationPulse : ISimulationUiPulse
{
    public event EventHandler? Tick { add { } remove { } }
    public void Start() { }
    public void Stop() { }
}
