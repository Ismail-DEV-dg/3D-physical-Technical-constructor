using System.Windows;

namespace CircuitEditor.App.Services;

public sealed class WindowService : IWindowService
{
    public void Minimize()
    {
        if (Application.Current.MainWindow is { } window)
            window.WindowState = WindowState.Minimized;
    }

    public void ToggleMaximize()
    {
        if (Application.Current.MainWindow is { } window)
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal : WindowState.Maximized;
    }

    public void Close() => Application.Current.MainWindow?.Close();
}

