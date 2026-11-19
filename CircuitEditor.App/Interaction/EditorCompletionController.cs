using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using CircuitEditor.App.ViewModels;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
namespace CircuitEditor.App.Interaction;
internal sealed class EditorCompletionController : IDisposable
{
    private const int MinimumPrefixLength=2,MaximumPopupHeight=260,MinimumPopupWidth=220;
    private readonly TextEditor editor;
    private readonly CodeViewModel model;
    private CompletionWindow? popup;
    internal EditorCompletionController(TextEditor editor,CodeViewModel model)
    {this.editor=editor;this.model=model;editor.TextArea.TextEntered+=Entered;editor.TextArea.TextEntering+=Entering;editor.PreviewKeyDown+=KeyDown;}
    private void KeyDown(object sender,KeyEventArgs e)
    {if(e.Key==Key.Space&&Keyboard.Modifiers==ModifierKeys.Control){Open(true);e.Handled=true;}}
    private void Entering(object? sender,TextCompositionEventArgs e)
    {if(!e.Handled&&popup is not null&&e.Text.Length>0&&!char.IsLetterOrDigit(e.Text[0])&&e.Text[0]!='_')popup.CompletionList.RequestInsertion(e);}
    private void Entered(object? sender,TextCompositionEventArgs e)
    {if(e.Text.Length==1&&(char.IsLetter(e.Text[0])||e.Text[0]=='_'))Open(false);}
    internal CompletionWindow? Popup=>popup;
    internal void Open(bool explicitRequest)
    {
        if(popup is not null)return;
        var start=editor.CaretOffset;
        while(start>0&&(char.IsLetterOrDigit(editor.Text[start-1])||editor.Text[start-1]=='_'))start--;
        var prefix=editor.Text[start..editor.CaretOffset];if(!explicitRequest&&prefix.Length<MinimumPrefixLength)return;
        if(!EditorTypingController.IsCode(editor.Text,start))return;
        var items=model.Completions.Where(i=>i.Text.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)).ToArray();if(items.Length==0)return;
        popup=new CompletionWindow(editor.TextArea){StartOffset=start,FontFamily=editor.FontFamily,FontSize=editor.FontSize,
            WindowStyle=WindowStyle.None,AllowsTransparency=true,ResizeMode=ResizeMode.NoResize,MaxHeight=MaximumPopupHeight,MinWidth=MinimumPopupWidth,
            Template=(ControlTemplate)editor.FindResource("CodeCompletionWindowTemplate")};
        foreach(var property in new[]{"Background","Foreground","BorderBrush"})
            popup.SetResourceReference(property=="Background"?System.Windows.Controls.Control.BackgroundProperty:
                property=="Foreground"?System.Windows.Controls.Control.ForegroundProperty:System.Windows.Controls.Control.BorderBrushProperty,
                property=="Background"?"PanelBrush":property=="Foreground"?"TextBrush":"AccentBrush");
        foreach(var item in items)popup.CompletionList.CompletionData.Add(new EditorCompletionData(item));
        popup.Closed+=Closed;popup.Show();popup.CompletionList.ApplyTemplate();
        popup.CompletionList.ListBox.Style=(Style)editor.FindResource(typeof(ListBox));
        popup.CompletionList.ListBox.ItemContainerStyle=(Style)editor.FindResource(typeof(ListBoxItem));
        popup.SizeToContent=SizeToContent.WidthAndHeight;
    }
    private void Closed(object? sender,EventArgs e){if(popup is not null)popup.Closed-=Closed;popup=null;}
    internal void Dismiss()=>popup?.Close();
    public void Dispose()
    {editor.TextArea.TextEntered-=Entered;editor.TextArea.TextEntering-=Entering;editor.PreviewKeyDown-=KeyDown;popup?.Close();}
}
