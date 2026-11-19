using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
namespace CircuitEditor.App.Interaction;
internal static class EditorHighlighting
{
    internal static void Apply(TextEditor editor)
    {
        using var stream=Application.GetResourceStream(new Uri("pack://application:,,,/CircuitEditor.App;component/Resources/Code/Arduino.xshd"))!.Stream;
        using var reader=new StreamReader(stream);var text=reader.ReadToEnd();
        foreach(var key in new[]{"Muted","Green","Purple","Accent","Text"})
        {var color=((SolidColorBrush)editor.FindResource(key+"Brush")).Color.ToString();text=text.Replace("{"+key+"}",color,StringComparison.Ordinal);}
        using var xml=XmlReader.Create(new StringReader(text));editor.SyntaxHighlighting=HighlightingLoader.Load(xml,null);
        editor.TextArea.SelectionBrush=(Brush)editor.FindResource("HoverBrush");editor.TextArea.SelectionForeground=(Brush)editor.FindResource("TextBrush");
        editor.TextArea.Caret.CaretBrush=(Brush)editor.FindResource("AccentBrush");
        editor.TextArea.TextView.CurrentLineBackground=(Brush)editor.FindResource("RaisedBrush");
        editor.TextArea.TextView.CurrentLineBorder=new Pen((Brush)editor.FindResource("LineBrush"),1);
        Brush Frozen(string name){var brush=((Brush)editor.FindResource(name)).CloneCurrentValue();brush.Freeze();return brush;}
        ICSharpCode.AvalonEdit.Folding.FoldingMargin.SetFoldingMarkerBrush(editor.TextArea,Frozen("AccentBrush"));
        ICSharpCode.AvalonEdit.Folding.FoldingMargin.SetFoldingMarkerBackgroundBrush(editor.TextArea,Frozen("PanelBrush"));
    }
}
