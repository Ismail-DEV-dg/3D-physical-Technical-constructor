using System.Windows.Media;
using CircuitEditor.Core.Code;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
namespace CircuitEditor.App.Interaction;
internal sealed class EditorCompletionData(CodeCompletionItem item) : ICompletionData
{
    public ImageSource? Image=>null;
    public string Text=>item.Text;
    public object Content=>item.Text;
    public object Description=>item.Description;
    public double Priority=>item.Category=="Project"?1:0;
    public void Complete(TextArea textArea,ISegment completionSegment,EventArgs insertionRequestEventArgs)=>
        textArea.Document.Replace(completionSegment,item.Text);
}
