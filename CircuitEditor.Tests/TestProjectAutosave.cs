using CircuitEditor.App.Services;

namespace CircuitEditor.Tests;

internal sealed class TestProjectAutosave : IAutosaveService
{
    public void Start(Action save) { }
    public void Dispose() { }
}
