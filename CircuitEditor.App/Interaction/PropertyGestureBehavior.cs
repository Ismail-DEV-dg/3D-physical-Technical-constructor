using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CircuitEditor.App.ViewModels;
namespace CircuitEditor.App.Interaction;
public static class PropertyGestureBehavior
{
    public static readonly DependencyProperty ProjectProperty = DependencyProperty.RegisterAttached("Project",typeof(ProjectViewModel),typeof(PropertyGestureBehavior),
        new FrameworkPropertyMetadata(null,FrameworkPropertyMetadataOptions.Inherits));
    public static void SetProject(DependencyObject o,ProjectViewModel v) => o.SetValue(ProjectProperty,v);
    public static ProjectViewModel? GetProject(DependencyObject o) => (ProjectViewModel?)o.GetValue(ProjectProperty);
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached("Enabled",typeof(bool),typeof(PropertyGestureBehavior),new PropertyMetadata(false,Changed));
    public static void SetEnabled(DependencyObject o,bool v) => o.SetValue(EnabledProperty,v);
    public static bool GetEnabled(DependencyObject o) => (bool)o.GetValue(EnabledProperty);
    private static void Changed(DependencyObject o,DependencyPropertyChangedEventArgs e)
    {
        if (o is not Slider slider) return;
        if ((bool)e.NewValue)
        {
            slider.PreviewMouseLeftButtonDown+=Down; slider.PreviewMouseLeftButtonUp+=Up;
            slider.LostMouseCapture+=Lost; slider.PreviewKeyDown+=KeyDown; slider.PreviewKeyUp+=KeyUp; slider.LostKeyboardFocus+=FocusLost;
        }
        else
        {
            slider.PreviewMouseLeftButtonDown-=Down; slider.PreviewMouseLeftButtonUp-=Up;
            slider.LostMouseCapture-=Lost; slider.PreviewKeyDown-=KeyDown; slider.PreviewKeyUp-=KeyUp; slider.LostKeyboardFocus-=FocusLost;
        }
    }
    private static void Down(object s,MouseButtonEventArgs e) => GetProject((DependencyObject)s)?.BeginGesture("Изменение параметра");
    private static void Up(object s,MouseButtonEventArgs e) => GetProject((DependencyObject)s)?.EndGesture();
    private static void Lost(object s,MouseEventArgs e) { if (e.LeftButton == MouseButtonState.Released) GetProject((DependencyObject)s)?.EndGesture(); }
    private static void KeyDown(object s,KeyEventArgs e)
    {
        var p=GetProject((DependencyObject)s);
        if (e.Key==Key.Escape) { p?.CancelGesture(); e.Handled=true; }
        else if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End or Key.PageUp or Key.PageDown) p?.BeginGesture("Изменение параметра");
    }
    private static void KeyUp(object s,KeyEventArgs e) => GetProject((DependencyObject)s)?.EndGesture();
    private static void FocusLost(object s,KeyboardFocusChangedEventArgs e) => GetProject((DependencyObject)s)?.EndGesture();
}
