using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using CircuitEditor.App.ViewModels;
namespace CircuitEditor.App.Interaction;
public static class ProjectShortcutBehavior
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached("Enabled",typeof(bool),typeof(ProjectShortcutBehavior),
        new PropertyMetadata(false,(o,e) => { if (o is Window w) { if ((bool)e.NewValue) w.PreviewKeyDown += KeyDown; else w.PreviewKeyDown -= KeyDown; } }));
    public static void SetEnabled(DependencyObject o, bool v) => o.SetValue(EnabledProperty,v);
    public static bool GetEnabled(DependencyObject o) => (bool)o.GetValue(EnabledProperty);
    private static void KeyDown(object sender, KeyEventArgs e)
    {
        if (((Window)sender).DataContext is not MainViewModel vm || !Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) return;
        ICommand? command = e.Key switch
        {
            Key.S => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? vm.Session?.SaveAsCommand : vm.Session?.SaveCommand,
            Key.O => vm.Session?.OpenCommand,
            Key.N => vm.Session?.NewCommand,
            _ => null
        };
        if (Keyboard.FocusedElement is not TextBoxBase)
            command ??= e.Key switch
            {
                Key.Z => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? vm.Project.RedoCommand : vm.Project.UndoCommand,
                Key.Y => vm.Project.RedoCommand,
                Key.C => vm.Project.CopyCommand,
                Key.V => vm.Project.PasteCommand,
                _ => null
            };
        if (command is null) return;
        if (command.CanExecute(null)) command.Execute(null);
        e.Handled=true;
    }
}
