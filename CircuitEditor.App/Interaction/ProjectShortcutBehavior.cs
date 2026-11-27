using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using CircuitEditor.App.Controls;
using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Views;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
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
        var parent=focus as DependencyObject;var codeFocus=false;var editorFocus=false;
        while(parent is not null)
        {
            if(parent is DiagnosticsView or OutputView&&Keyboard.Modifiers==ModifierKeys.Control&&e.Key==Key.C)return;
            if(parent is TextEditor) editorFocus=true;
            if(parent is CodeView){codeFocus=true;break;}parent=parent is Visual?VisualTreeHelper.GetParent(parent):LogicalTreeHelper.GetParent(parent);
        }
        if(codeFocus&&e.Key==Key.Escape){vm.Code.CloseSearchCommand.Execute(null);e.Handled=true;return;}
        if(codeFocus&&e.Key==Key.Enter&&focus is TextBox {Name: "CodeFind" or "CodeLine" or "CodeReplacement"} box)
        {vm.Code.EditorActionCommand.Execute(box.Name=="CodeLine"?"GoLine":box.Name=="CodeReplacement"?"ReplaceOne":"FindNext");e.Handled=true;return;}
        var editing=codeFocus||focus is TextBoxBase or PasswordBox || focus is ComboBox {IsEditable:true};
        var canvas=focus is BreadboardSurface or SchematicSurface;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.ImeProcessed || key == Key.DeadCharProcessed) return;
        var binding = vm.Shortcuts.Bindings.FirstOrDefault(b => b.Key == key && b.Modifiers == Keyboard.Modifiers &&
            b.MatchesContext(editing, canvas || vm.IsMockup && vm.Mockup.IsPlacementArmed && b.Id is ("Rotate" or "Cancel"),
                codeFocus, editorFocus));
        if(binding is null)return;
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
