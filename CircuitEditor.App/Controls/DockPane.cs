using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CircuitEditor.App.ViewModels;

namespace CircuitEditor.App.Controls;

public sealed class DockPane : ContentControl
{
    public static readonly DependencyProperty PaneProperty = DependencyProperty.Register(
        nameof(Pane), typeof(DockPaneViewModel), typeof(DockPane));
    public DockPaneViewModel? Pane
    {
        get => (DockPaneViewModel?)GetValue(PaneProperty);
        set => SetValue(PaneProperty, value);
    }
    public static readonly DependencyProperty IsBottomProperty = DependencyProperty.Register(
        nameof(IsBottom), typeof(bool), typeof(DockPane), new PropertyMetadata(false));
    public bool IsBottom
    {
        get => (bool)GetValue(IsBottomProperty);
        set => SetValue(IsBottomProperty, value);
    }
    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (!IsKeyboardFocusWithin && !IsMouseCaptureWithin) Pane?.CollapseIfUnpinned();
    }
    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);
        if (!IsKeyboardFocusWithin && !IsMouseOver && !IsMouseCaptureWithin) Pane?.CollapseIfUnpinned();
    }
}
