using System.Windows;

namespace CircuitEditor.App.Services;

internal interface ISceneBenchmark
{
    Window CreateWindow(string reportPath);
}
