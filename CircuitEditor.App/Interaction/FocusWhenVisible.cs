using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace CircuitEditor.App.Interaction;

public static class FocusWhenVisible
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(FocusWhenVisible), new PropertyMetadata(false, OnEnabledChanged));

    public static void SetEnabled(DependencyObject element, bool value) => element.SetValue(EnabledProperty, value);
    public static bool GetEnabled(DependencyObject element) => (bool)element.GetValue(EnabledProperty);

    private static void OnEnabledChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not FrameworkElement element) return;
        if ((bool)args.NewValue) element.IsVisibleChanged += OnVisibleChanged;
        else element.IsVisibleChanged -= OnVisibleChanged;
    }

    private static void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is FrameworkElement { IsVisible: true } element)
            element.Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
            {
                if (element.IsVisible) Keyboard.Focus(element);
            });
    }
}
