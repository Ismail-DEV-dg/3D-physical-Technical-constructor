using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;
using CircuitEditor.Simulation.Engine;

namespace CircuitEditor.App.Services;

public interface ISimulationPlayback
{
    bool HasSession { get; }
    bool IsRunning { get; }
    double Speed { get; set; }
    SimulationState? TakeFrame();
    void Start(CircuitProject project, ScenarioConfiguration? scenario, bool run);
    void Resume();
    void Pause();
    void Step();
    void Stop();
    void Send(Action<SimulationSession> action);
    void SetLatest(object key, Action<SimulationSession> action);
}
