using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CircuitEditor.App.Interaction;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Views;
using CircuitEditor.Core.Code;
using CircuitEditor.Infrastructure.Repositories;
using ICSharpCode.AvalonEdit;
namespace CircuitEditor.Tests;
internal static class CodeEditorPresentationChecks
{
    internal static void Run(ResourceDictionary resources)
    {
        Application.Current.Resources.MergedDictionaries.Add(resources);
        using var e=new ElectricalTestCircuit();e.Add("arduino-uno");
        var themes=new ThemeService(new WpfThemeResources(resources));
        using var vm=new CodeViewModel(e.Project,new JsonCodeCompletionRepository(),new JsonCodeTemplateRepository(),themes);
        var view=new CodeView {DataContext=vm};var window=new Window {Content=view,Width=950,Height=650,Left=-10000,Top=-10000,ShowInTaskbar=false};
        try
        {
        window.Show();window.UpdateLayout();var editor=(TextEditor)view.FindName("Editor");
        Assert.NotNull(editor.SyntaxHighlighting);Assert.True(editor.ShowLineNumbers);
        Assert.True(editor.TextArea.IndentationStrategy is ICSharpCode.AvalonEdit.Indentation.CSharp.CSharpIndentationStrategy);
        vm.Text="alpha alpha\nvoid setup() {\n  // { ignored }\n}\n";
        Assert.Equal(vm.Text,editor.Text);Assert.True(editor.CanUndo);
        vm.SearchText="alpha";vm.ReplacementText="beta";vm.EditorActionCommand.Execute("ReplaceAll");
        Assert.StartsWith("beta beta",vm.Text);editor.Undo();Assert.StartsWith("alpha alpha",vm.Text);
        editor.CaretOffset=0;vm.EditorActionCommand.Execute("FindNext");Assert.Equal("alpha",editor.SelectedText);
        vm.GoToText="3";vm.EditorActionCommand.Execute("GoLine");Assert.Equal(3,vm.CursorLine);
        vm.EnsureEntryPoints();Assert.Contains("void setup()",vm.Text);Assert.Contains("void loop()",vm.Text);
        vm.Text="";editor.CaretOffset=0;editor.Focus();
        Type(editor,"(");Assert.Equal("()",vm.Text);Assert.Equal(1,editor.CaretOffset);
        Type(editor,")");Assert.Equal("()",vm.Text);Assert.Equal(2,editor.CaretOffset);
        vm.Text="";Type(editor,"{");
        editor.TextArea.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(window),0,Key.Enter){RoutedEvent=Keyboard.PreviewKeyDownEvent});
        Assert.Contains("{\n  \n}",vm.Text);Assert.Equal(2,editor.TextArea.Caret.Line);
        Assert.False(EditorTypingController.IsCode("// comment",10));Assert.False(EditorTypingController.IsCode("\"string",7));
        Assert.Single(EditorFoldingController.Ranges("void f() {\n const char* s=\"}\"; // }\n}\n"));
        Assert.Empty(EditorFoldingController.Ranges("// {\n// }"));
        vm.Text = "first\r\nsecond\r\nthird";
        editor.Select(7, 0);
        vm.EditorActionCommand.Execute("DuplicateLine");
        Assert.Equal("first\r\nsecond\r\nsecond\r\nthird", vm.Text);
        Assert.Equal(3, editor.TextArea.Caret.Line);
        editor.Undo();
        Assert.Equal("first\r\nsecond\r\nthird", vm.Text);
        editor.Select(0, 15);
        vm.EditorActionCommand.Execute("MoveLineDown");
        Assert.Equal("third\r\nfirst\r\nsecond", vm.Text);
        Assert.Equal("first\r\nsecond", editor.SelectedText);
        vm.EditorActionCommand.Execute("MoveLineUp");
        Assert.Equal("first\r\nsecond\r\nthird", vm.Text);
        editor.Select(7, 8);
        vm.EditorActionCommand.Execute("ToggleComment");
        Assert.Equal("first\r\n// second\r\nthird", vm.Text);
        vm.EditorActionCommand.Execute("ToggleComment");
        Assert.Equal("first\r\nsecond\r\nthird", vm.Text);
        vm.EditorActionCommand.Execute("DeleteLine");
        Assert.Equal("first\r\nthird", vm.Text);
        var document=editor.Document;
        window.Content=null;window.UpdateLayout();window.Content=new CodeView {DataContext=vm};window.UpdateLayout();
        window.Dispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.Loaded);
        var second=(TextEditor)((CodeView)window.Content).FindName("Editor");Assert.Same(document,second.Document);Assert.True(second.CanUndo);
        var oldColor=second.SyntaxHighlighting.GetNamedColor("Keyword").Foreground.GetColor(null);
        themes.SetTheme(ThemeKind.LightTech);Assert.NotEqual(oldColor,second.SyntaxHighlighting.GetNamedColor("Keyword").Foreground.GetColor(null));
        e.Project.ReplaceProject(new Core.Models.CircuitProject(footprints:e.Project.Model.Footprints,breadboards:e.Project.Model.Breadboards));
        Assert.Equal(SketchDocument.StarterText, vm.Text);Assert.Equal(vm.Text,second.Text);Assert.False(second.CanUndo);
        }
        finally {themes.SetTheme(ThemeKind.NeonDark);window.Close();Application.Current.Resources.MergedDictionaries.Remove(resources);}
    }
    private static void Type(TextEditor editor,string text)
    {
        var composition=new TextComposition(InputManager.Current,editor.TextArea,text);
        editor.TextArea.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice,composition){RoutedEvent=TextCompositionManager.TextInputEvent});
    }
}
