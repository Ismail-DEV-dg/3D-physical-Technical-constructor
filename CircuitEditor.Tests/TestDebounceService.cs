using CircuitEditor.App.Services;
namespace CircuitEditor.Tests;
internal sealed class TestDebounceService : IDebounceService
{
    public Action? Pending {get;private set;}
    public void Schedule(Action action)=>Pending=action;
    public void Flush(){var action=Pending;Pending=null;action?.Invoke();}
    public void Dispose()=>Pending=null;
}
