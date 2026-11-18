using System.Windows.Threading;
namespace CircuitEditor.App.Services;
public sealed class DebounceService : IDebounceService
{
    private readonly DispatcherTimer timer=new() {Interval=TimeSpan.FromMilliseconds(350)};
    private Action? pending;
    public DebounceService()=>timer.Tick+=Tick;
    public void Schedule(Action action){timer.Stop();pending=action;timer.Start();}
    private void Tick(object? sender,EventArgs e){timer.Stop();var action=pending;pending=null;action?.Invoke();}
    public void Dispose(){timer.Stop();timer.Tick-=Tick;pending=null;}
}
