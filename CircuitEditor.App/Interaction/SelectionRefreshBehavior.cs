using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace CircuitEditor.App.Interaction;

// Re-read the validated source after the selection transaction ends.
public static class SelectionRefreshBehavior
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(SelectionRefreshBehavior), new PropertyMetadata(false, Changed));
    public static void SetEnabled(DependencyObject element, bool value) => element.SetValue(EnabledProperty, value);
    public static bool GetEnabled(DependencyObject element) => (bool)element.GetValue(EnabledProperty);
    private static void Changed(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not ComboBox combo) return;
        if ((bool)args.NewValue) combo.DropDownClosed += Refresh;
        else combo.DropDownClosed -= Refresh;
    }
    private static void Refresh(object? sender, EventArgs args) =>
        ((ComboBox)sender!).GetBindingExpression(Selector.SelectedItemProperty)?.UpdateTarget();
}
