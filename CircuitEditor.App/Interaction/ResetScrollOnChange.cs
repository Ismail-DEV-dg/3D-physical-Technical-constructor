using System.Windows;
using System.Windows.Controls;

namespace CircuitEditor.App.Interaction;

public static class ResetScrollOnChange
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached(
        "Value", typeof(object), typeof(ResetScrollOnChange), new PropertyMetadata(null,
            (target, _) => { if (target is ScrollViewer viewer) viewer.ScrollToTop(); }));

    public static void SetValue(DependencyObject target, object? value) => target.SetValue(ValueProperty, value);
    public static object? GetValue(DependencyObject target) => target.GetValue(ValueProperty);
}
