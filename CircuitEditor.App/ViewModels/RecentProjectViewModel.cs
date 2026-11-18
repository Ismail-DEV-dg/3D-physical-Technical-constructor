using System.IO;
using System.Windows.Input;
namespace CircuitEditor.App.ViewModels;
public sealed class RecentProjectViewModel(string path, ICommand openCommand)
{
    public string Path => path;
    public string Name => System.IO.Path.GetFileName(path);
    public ICommand OpenCommand => openCommand;
}
