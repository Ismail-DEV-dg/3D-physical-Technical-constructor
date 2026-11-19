namespace CircuitEditor.App.Services;

public interface ISimulationUiPulse
{
    event EventHandler? Tick;
    void Start();
    void Stop();
}
