using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;

namespace CircuitEditor.App.Interaction;

public static class InstrumentDragBehavior
{
    public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
        "Command", typeof(ICommand), typeof(InstrumentDragBehavior), new PropertyMetadata(null, Changed));

    public static void SetCommand(DependencyObject element, ICommand value) => element.SetValue(CommandProperty, value);
    public static ICommand? GetCommand(DependencyObject element) => (ICommand?)element.GetValue(CommandProperty);

    private static void Changed(DependencyObject element, DependencyPropertyChangedEventArgs args)
    {
        if (element is not Thumb thumb) return;
        if (args.OldValue is not null) thumb.DragDelta -= Move;
        if (args.NewValue is not null) thumb.DragDelta += Move;
    }

    private static void Move(object sender, DragDeltaEventArgs args)
    {
        var thumb = (Thumb)sender;
        var panel = Find<ContentPresenter>(thumb);
        var surface = Find<ItemsControl>(thumb);
        if (panel is null || surface is null) return;
        var move = new InstrumentMove(args.HorizontalChange, args.VerticalChange,
            surface.ActualWidth - panel.ActualWidth, surface.ActualHeight - Math.Min(panel.ActualHeight, 40));
        var command = GetCommand(thumb);
        if (command?.CanExecute(move) == true) command.Execute(move);
        args.Handled = true;
    }

    private static T? Find<T>(DependencyObject start) where T : DependencyObject
    {
        for (var item = VisualTreeHelper.GetParent(start); item is not null; item = VisualTreeHelper.GetParent(item))
            if (item is T match) return match;
        return null;
    }
}
