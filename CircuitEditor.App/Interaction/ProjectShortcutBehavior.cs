using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using CircuitEditor.App.Controls;
using CircuitEditor.App.ViewModels;
namespace CircuitEditor.App.Interaction;
public static class ProjectShortcutBehavior
{
    public static readonly DependencyProperty EnabledProperty=DependencyProperty.RegisterAttached("Enabled",typeof(bool),typeof(ProjectShortcutBehavior),
        new PropertyMetadata(false,(o,e)=>{if(o is Window w){if((bool)e.NewValue)w.PreviewKeyDown+=KeyDown;else w.PreviewKeyDown-=KeyDown;}}));
    public static void SetEnabled(DependencyObject o,bool v)=>o.SetValue(EnabledProperty,v);
    public static bool GetEnabled(DependencyObject o)=>(bool)o.GetValue(EnabledProperty);
    private static void KeyDown(object sender,KeyEventArgs e)
    {
        var window=(Window)sender;if(window.DataContext is not MainViewModel vm)return;
        var focus=Keyboard.FocusedElement;
        var editing=focus is TextBoxBase or PasswordBox || focus is ComboBox {IsEditable:true};
        var canvas=focus is BreadboardSurface or SchematicSurface;
        var binding=ShortcutCatalog.Resolve(e.Key==Key.System?e.SystemKey:e.Key,Keyboard.Modifiers,editing,canvas);if(binding is null)return;
        if(binding.Id=="Search" && vm.Project.InGesture){vm.StatusMessage="Завершите жест или нажмите Esc перед поиском.";e.Handled=true;return;}
        if(binding.Id=="Search")
        {
            vm.Panels.Components.ShowCommand.Execute(null);window.UpdateLayout();
            if((window.FindName("EditorLayout") as FrameworkElement)?.FindName("PaletteSearch") is TextBox search)
            {search.Focus();search.SelectAll();}
        }
        else ShortcutCommands.Execute(vm,binding.Id);
        e.Handled=true;
    }
}
