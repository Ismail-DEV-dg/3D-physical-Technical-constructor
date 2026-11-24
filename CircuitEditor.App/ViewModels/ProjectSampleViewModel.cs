using System.Windows.Input;
using CircuitEditor.Core.Documents;

namespace CircuitEditor.App.ViewModels;

public sealed record ProjectSampleViewModel(ProjectSample Sample, ICommand OpenCommand)
{
    public string Title => Sample.Title;
    public string Description => Sample.Description;
}
