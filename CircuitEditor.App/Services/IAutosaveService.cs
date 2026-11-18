namespace CircuitEditor.App.Services;
public interface IAutosaveService : IDisposable
{
    void Start(Action save);
}
