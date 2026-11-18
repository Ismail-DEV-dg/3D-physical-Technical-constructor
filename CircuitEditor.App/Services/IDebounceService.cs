namespace CircuitEditor.App.Services;
public interface IDebounceService : IDisposable
{
    void Schedule(Action action);
}
