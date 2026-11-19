using System.Windows.Threading;

namespace CircuitEditor.App.Services;

public sealed class SimulationUiPulse : ISimulationUiPulse
{
    private readonly DispatcherTimer timer = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(33) };

    public event EventHandler? Tick { add => timer.Tick += value; remove => timer.Tick -= value; }
    public void Start() => timer.Start();
    public void Stop() => timer.Stop();
}
