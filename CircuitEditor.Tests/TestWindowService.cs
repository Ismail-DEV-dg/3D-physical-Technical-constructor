using CircuitEditor.App.Services;

namespace CircuitEditor.Tests;

internal sealed class TestWindowService : IWindowService
{
    public void Minimize() { }
    public void ToggleMaximize() { }
    public void Close() { }
}
