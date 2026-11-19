using System.Windows;
using CircuitEditor.App.ViewModels;
using ICSharpCode.AvalonEdit;
namespace CircuitEditor.App.Interaction;
public static class CodeEditorBehavior
{
    public static readonly DependencyProperty ModelProperty=DependencyProperty.RegisterAttached("Model",typeof(CodeViewModel),typeof(CodeEditorBehavior),new PropertyMetadata(null,Changed));
    private static readonly DependencyProperty SessionProperty=DependencyProperty.RegisterAttached("Session",typeof(CodeEditorSession),typeof(CodeEditorBehavior));
    public static void SetModel(DependencyObject o,CodeViewModel value)=>o.SetValue(ModelProperty,value);
    public static CodeViewModel? GetModel(DependencyObject o)=>(CodeViewModel?)o.GetValue(ModelProperty);
    private static void Changed(DependencyObject o,DependencyPropertyChangedEventArgs e)
    {
        if(o is not TextEditor editor)return;
        editor.Loaded-=Loaded;editor.Unloaded-=Unloaded;Detach(editor);
        if(e.NewValue is CodeViewModel){editor.Loaded+=Loaded;editor.Unloaded+=Unloaded;if(editor.IsLoaded)Attach(editor);}
    }
    private static void Loaded(object sender,RoutedEventArgs e)=>Attach((TextEditor)sender);
    private static void Unloaded(object sender,RoutedEventArgs e)=>Detach((TextEditor)sender);
    private static void Attach(TextEditor editor)
    {if(editor.GetValue(SessionProperty) is null&&GetModel(editor) is { } model)editor.SetValue(SessionProperty,new CodeEditorSession(editor,model));}
    private static void Detach(TextEditor editor)
    {(editor.GetValue(SessionProperty) as CodeEditorSession)?.Dispose();editor.ClearValue(SessionProperty);}
}
