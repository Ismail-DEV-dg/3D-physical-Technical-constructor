using System.ComponentModel;
using System.Windows.Input;
using CircuitEditor.App.Interaction;

namespace CircuitEditor.App.Services;

public interface IShortcutSettings : INotifyPropertyChanged
{
    IReadOnlyList<ShortcutBinding> Bindings { get; }
    string this[string command] { get; }
    string? Assign(ShortcutBinding binding, Key key, ModifierKeys modifiers);
    string? Restore(ShortcutBinding binding);
    string? RestoreAll();
}
