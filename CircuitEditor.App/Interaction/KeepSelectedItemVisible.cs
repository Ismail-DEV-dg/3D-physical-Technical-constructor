using System.Windows;
using System.Windows.Controls;

namespace CircuitEditor.App.Interaction;

public static class KeepSelectedItemVisible
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(KeepSelectedItemVisible), new PropertyMetadata(false, Changed));

    public static void SetEnabled(DependencyObject target, bool value) => target.SetValue(EnabledProperty, value);
    public static bool GetEnabled(DependencyObject target) => (bool)target.GetValue(EnabledProperty);

    private static void Changed(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not ListBox list) return;
        if ((bool)args.NewValue) list.SelectionChanged += SelectionChanged;
        else list.SelectionChanged -= SelectionChanged;
    }

    private static void SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        var list = (ListBox)sender;
        if (ReferenceEquals(args.Source, list) && list.SelectedItem is { } selected) list.ScrollIntoView(selected);
    }
}
