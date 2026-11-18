using System.Windows.Threading;
namespace CircuitEditor.App.Services;
public sealed class AutosaveService : IAutosaveService
{
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMinutes(1) };
    private Action? save;
    public AutosaveService() => timer.Tick += OnTick;
    private void OnTick(object? sender, EventArgs args) => save?.Invoke();
    public void Start(Action callback) { save = callback; timer.Start(); }
    public void Dispose() { timer.Stop(); timer.Tick -= OnTick; save = null; }
}
