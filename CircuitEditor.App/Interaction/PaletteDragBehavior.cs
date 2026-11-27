using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using CircuitEditor.App.ViewModels;

namespace CircuitEditor.App.Interaction;

// WPF drag transport only; the receiving ViewModel creates the component.
public static class PaletteDragBehavior
{
    [ThreadStatic] internal static PaletteDragPayload? ActiveDrag;
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(PaletteDragBehavior), new PropertyMetadata(false, Changed));
    private static readonly DependencyProperty StartProperty = DependencyProperty.RegisterAttached(
        "Start", typeof(Point?), typeof(PaletteDragBehavior));
    private static readonly DependencyProperty DefinitionIdProperty = DependencyProperty.RegisterAttached(
        "DefinitionId", typeof(string), typeof(PaletteDragBehavior));
    public static readonly DependencyProperty QuickItemProperty = DependencyProperty.RegisterAttached(
        "QuickItem", typeof(string), typeof(PaletteDragBehavior), new PropertyMetadata(null, QuickItemChanged));
    public static void SetQuickItem(DependencyObject element, string value) => element.SetValue(QuickItemProperty, value);
    public static string? GetQuickItem(DependencyObject element) => (string?)element.GetValue(QuickItemProperty);
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
        var element = (FrameworkElement)sender;
        element.SetValue(StartProperty, (Point?)args.GetPosition(element));
        element.ClearValue(DefinitionIdProperty);
        if (GetQuickItem(element) is { } id) element.SetValue(DefinitionIdProperty, id);
        else if (element is ListBox list && ItemsControl.ContainerFromElement(list, args.OriginalSource as DependencyObject) is ListBoxItem
            { DataContext: PaletteItemViewModel item }) element.SetValue(DefinitionIdProperty, item.Definition.Id);
    }

    private static void QuickItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not FrameworkElement element) return;
        if (args.OldValue is not null) { element.PreviewMouseLeftButtonDown -= Down; element.PreviewMouseMove -= Move; }
        if (args.NewValue is not null) { element.PreviewMouseLeftButtonDown += Down; element.PreviewMouseMove += Move; }
    }

    private static void Move(object sender, MouseEventArgs args)
    {
        var list = (FrameworkElement)sender;
        if (args.LeftButton != MouseButtonState.Pressed || list.GetValue(StartProperty) is not Point start) return;
        var point = args.GetPosition(list);
        if (Math.Abs(point.X - start.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(point.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        list.ClearValue(StartProperty);
        if (list.GetValue(DefinitionIdProperty) is not string id) return;
        var payload = new PaletteDragPayload(id);
        var rotate = (Window.GetWindow(list)?.DataContext as MainViewModel)?.Shortcuts.Bindings.FirstOrDefault(binding => binding.Id == "Rotate");
        var key = KeyInterop.VirtualKeyFromKey(rotate?.Key ?? Key.R);
        var modifiers = rotate?.Modifiers ?? ModifierKeys.None;
        bool RotationKeyDown() => Pressed(key) &&
            Pressed(0x11) == modifiers.HasFlag(ModifierKeys.Control) &&
            Pressed(0x10) == modifiers.HasFlag(ModifierKeys.Shift) &&
            Pressed(0x12) == modifiers.HasFlag(ModifierKeys.Alt);
        var wasDown = RotationKeyDown();
        var timer = new DispatcherTimer(DispatcherPriority.Input) { Interval = TimeSpan.FromMilliseconds(20) };
        timer.Tick += (_, _) =>
        {
            // Native OLE drag owns keyboard input, so normal WPF KeyDown does not reach the canvas.
            var down = RotationKeyDown();
            if (down && !wasDown) payload.Rotate();
            wasDown = down;
        };
        timer.Start();
        ActiveDrag = payload;
        try { DragDrop.DoDragDrop(list, new DataObject(CanvasInputBehavior.DragFormat, id), DragDropEffects.Copy); }
        finally
        {
            timer.Stop();
            ActiveDrag = null;
            payload.RefreshTarget?.Invoke(true);
        }
    }

    private static bool Pressed(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern short GetAsyncKeyState(int virtualKey);
}
