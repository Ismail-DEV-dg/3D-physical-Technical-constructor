using System.Windows;
using System.Windows.Input;

namespace CircuitEditor.App.Interaction;

// Input adapter only: coordinate conversion, selection and edits belong to the ViewModel.
public static class CanvasInputBehavior
{
    public const string DragFormat = "CircuitEditor.ComponentDefinitionId";
    public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
        "Command", typeof(ICommand), typeof(CanvasInputBehavior), new PropertyMetadata(null, OnCommandChanged));
    public static void SetCommand(DependencyObject element, ICommand value) => element.SetValue(CommandProperty, value);
    public static ICommand? GetCommand(DependencyObject element) => (ICommand?)element.GetValue(CommandProperty);

    private static void OnCommandChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not FrameworkElement element) return;
        if (args.OldValue is not null)
        {
            element.MouseDown -= Down; element.MouseMove -= Move; element.MouseUp -= Up;
            element.MouseWheel -= Wheel; element.KeyDown -= KeyDown;
            element.Drop -= Drop; element.DragOver -= DragOver; element.LostMouseCapture -= LostCapture;
        }
        if (args.NewValue is null) return;
        element.Focusable = true; element.AllowDrop = true;
        element.MouseDown += Down; element.MouseMove += Move; element.MouseUp += Up;
        element.MouseWheel += Wheel; element.KeyDown += KeyDown;
        element.Drop += Drop; element.DragOver += DragOver; element.LostMouseCapture += LostCapture;
    }

    private static void Send(FrameworkElement element, CanvasInput input)
    {
        var command = GetCommand(element);
        if (command?.CanExecute(input) == true) command.Execute(input);
    }

    private static CanvasInput Pointer(FrameworkElement element, MouseEventArgs args, CanvasAction action, int delta = 0)
    {
        var point = args.GetPosition(element);
        return new CanvasInput(action, point.X, point.Y, delta,
            Keyboard.Modifiers.HasFlag(ModifierKeys.Control));
    }

    private static void Down(object sender, MouseButtonEventArgs args)
    {
        var element = (FrameworkElement)sender;
        element.Focus();
        element.CaptureMouse();
        var input = Pointer(element, args, CanvasAction.Down);
        Send(element, input with { Pan = args.ChangedButton is MouseButton.Middle or MouseButton.Right
            || Keyboard.IsKeyDown(Key.Space) });
        args.Handled = true;
    }

    private static void Move(object sender, MouseEventArgs args) =>
        Send((FrameworkElement)sender, Pointer((FrameworkElement)sender, args, CanvasAction.Move));

    private static void Up(object sender, MouseButtonEventArgs args)
    {
        var element = (FrameworkElement)sender;
        Send(element, Pointer(element, args, CanvasAction.Up));
        element.ReleaseMouseCapture();
        args.Handled = true;
    }

    private static void LostCapture(object sender, MouseEventArgs args) =>
        Send((FrameworkElement)sender, new CanvasInput(CanvasAction.Cancel));

    private static void Wheel(object sender, MouseWheelEventArgs args)
    {
        var element = (FrameworkElement)sender;
        Send(element, Pointer(element, args, CanvasAction.Wheel, args.Delta));
        args.Handled = true;
    }

    private static void KeyDown(object sender, KeyEventArgs args)
    {
        CanvasAction? action = args.Key switch
        {
            Key.R when Keyboard.Modifiers == ModifierKeys.None => CanvasAction.Rotate,
            Key.Delete => CanvasAction.Delete,
            Key.Escape => CanvasAction.Cancel,
            Key.A when Keyboard.Modifiers == ModifierKeys.Control => CanvasAction.SelectAll,
            _ => null
        };
        if (action is null) return;
        Send((FrameworkElement)sender, new CanvasInput(action.Value));
        args.Handled = true;
    }

    private static void DragOver(object sender, DragEventArgs args)
    {
        args.Effects = args.Data.GetDataPresent(DragFormat) ? DragDropEffects.Copy : DragDropEffects.None;
        args.Handled = true;
    }

    private static void Drop(object sender, DragEventArgs args)
    {
        var element = (FrameworkElement)sender;
        if (args.Data.GetData(DragFormat) is not string id) return;
        var point = args.GetPosition(element);
        Send(element, new CanvasInput(CanvasAction.Drop, point.X, point.Y, DefinitionId: id));
        element.Focus();
        args.Handled = true;
    }
}
