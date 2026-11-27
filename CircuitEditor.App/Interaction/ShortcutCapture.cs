using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CircuitEditor.App.ViewModels;

namespace CircuitEditor.App.Interaction;

public static class ShortcutCapture
{
    public static readonly DependencyProperty ModelProperty = DependencyProperty.RegisterAttached(
        "Model", typeof(ShortcutGuideViewModel), typeof(ShortcutCapture), new PropertyMetadata(null, Changed));
    private static readonly DependencyPropertyKey IsRecordingPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "IsRecording", typeof(bool), typeof(ShortcutCapture), new PropertyMetadata(false));
    public static readonly DependencyProperty IsRecordingProperty = IsRecordingPropertyKey.DependencyProperty;

    public static void SetModel(DependencyObject target, ShortcutGuideViewModel? value) => target.SetValue(ModelProperty, value);
    public static ShortcutGuideViewModel? GetModel(DependencyObject target) => (ShortcutGuideViewModel?)target.GetValue(ModelProperty);
    public static bool GetIsRecording(DependencyObject target) => (bool)target.GetValue(IsRecordingProperty);

    private static void Changed(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not Button button) return;
        if (args.OldValue is not null)
        {
            button.PreviewMouseLeftButtonDown -= SelectOrRecord;
            button.PreviewKeyDown -= Capture;
            button.LostKeyboardFocus -= LostFocus;
            button.DataContextChanged -= Recycled;
            End(button);
        }
        if (args.NewValue is not null)
        {
            button.PreviewMouseLeftButtonDown += SelectOrRecord;
            button.PreviewKeyDown += Capture;
            button.LostKeyboardFocus += LostFocus;
            button.DataContextChanged += Recycled;
        }
    }

    private static void SelectOrRecord(object sender, MouseButtonEventArgs args)
    {
        var button = (Button)sender;
        if (GetModel(button) is not { } model || button.DataContext is not ShortcutBinding binding) return;
        model.Selected = binding;
        if (args.ClickCount != 2 || !binding.CanCustomize) return;
        Begin(button, model);
        args.Handled = true;
    }

    private static void Capture(object sender, KeyEventArgs args)
    {
        var button = (Button)sender;
        if (GetModel(button) is not { } model || button.DataContext is not ShortcutBinding { CanCustomize: true } binding) return;
        var key = args.Key == Key.System ? args.SystemKey : args.Key;
        var modifiers = Keyboard.Modifiers;
        if (!GetIsRecording(button))
        {
            if (!args.IsRepeat && modifiers == ModifierKeys.None && key is Key.Enter or Key.Space)
            {
                model.Selected = binding;
                Begin(button, model);
                args.Handled = true;
            }
            return;
        }
        if (key == Key.Tab && modifiers is ModifierKeys.None or ModifierKeys.Shift) return;
        args.Handled = true;
        if (args.IsRepeat) return;
        if (key == Key.Escape)
        {
            End(button);
            model.Feedback = "";
            return;
        }
        if (key is Key.LeftAlt or Key.RightAlt or Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin) return;
        if (model.Assign(binding, key, modifiers)) End(button);
    }

    private static void Begin(Button button, ShortcutGuideViewModel model)
    {
        if (!button.Focus()) return;
        model.Feedback = "";
        button.SetValue(IsRecordingPropertyKey, true);
    }

    private static void LostFocus(object sender, KeyboardFocusChangedEventArgs args) => End((Button)sender);
    private static void Recycled(object sender, DependencyPropertyChangedEventArgs args) => End((Button)sender);
    private static void End(Button button) => button.SetValue(IsRecordingPropertyKey, false);
}
