using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CircuitEditor.App.ViewModels;

namespace CircuitEditor.App.Interaction;

// WPF drag transport only; the receiving ViewModel creates the component.
public static class PaletteDragBehavior
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(PaletteDragBehavior), new PropertyMetadata(false, Changed));
    private static readonly DependencyProperty StartProperty = DependencyProperty.RegisterAttached(
        "Start", typeof(Point?), typeof(PaletteDragBehavior));
    private static readonly DependencyProperty DefinitionIdProperty = DependencyProperty.RegisterAttached(
        "DefinitionId", typeof(string), typeof(PaletteDragBehavior));
    public static void SetEnabled(DependencyObject element, bool value) => element.SetValue(EnabledProperty, value);
    public static bool GetEnabled(DependencyObject element) => (bool)element.GetValue(EnabledProperty);

    private static void Changed(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not ListBox list) return;
        if ((bool)args.NewValue) { list.PreviewMouseLeftButtonDown += Down; list.PreviewMouseMove += Move; }
        else { list.PreviewMouseLeftButtonDown -= Down; list.PreviewMouseMove -= Move; }
    }

    private static void Down(object sender, MouseButtonEventArgs args)
    {
        var list = (ListBox)sender;
        list.SetValue(StartProperty, (Point?)args.GetPosition(list));
        list.ClearValue(DefinitionIdProperty);
        if (ItemsControl.ContainerFromElement(list, args.OriginalSource as DependencyObject) is ListBoxItem
            { DataContext: PaletteItemViewModel item }) list.SetValue(DefinitionIdProperty, item.Definition.Id);
    }

    private static void Move(object sender, MouseEventArgs args)
    {
        var list = (ListBox)sender;
        if (args.LeftButton != MouseButtonState.Pressed || list.GetValue(StartProperty) is not Point start) return;
        var point = args.GetPosition(list);
        if (Math.Abs(point.X - start.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(point.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        list.ClearValue(StartProperty);
        if (list.GetValue(DefinitionIdProperty) is not string id) return;
        DragDrop.DoDragDrop(list, new DataObject(CanvasInputBehavior.DragFormat, id), DragDropEffects.Copy);
    }
}
