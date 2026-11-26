using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CircuitEditor.App.ViewModels;

namespace CircuitEditor.App.Interaction;

public static class ShortcutCapture
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(ShortcutCapture), new PropertyMetadata(false, Changed));

    public static void SetEnabled(DependencyObject target, bool value) => target.SetValue(EnabledProperty, value);
    public static bool GetEnabled(DependencyObject target) => (bool)target.GetValue(EnabledProperty);

    private static void Changed(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not TextBox field) return;
        if ((bool)args.NewValue) field.PreviewKeyDown += Capture;
        else field.PreviewKeyDown -= Capture;
    }

    private static void Capture(object sender, KeyEventArgs args)
    {
        var key = args.Key == Key.System ? args.SystemKey : args.Key;
        var modifiers = Keyboard.Modifiers;
        if (key == Key.Escape || key == Key.Tab && modifiers is ModifierKeys.None or ModifierKeys.Shift) return;
        args.Handled = true;
        if (args.IsRepeat || key is Key.LeftAlt or Key.RightAlt or Key.LeftCtrl or Key.RightCtrl or
            Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin) return;
        if (((TextBox)sender).DataContext is ShortcutGuideViewModel model) model.Assign(key, modifiers);
    }
}
