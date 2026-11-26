using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Code;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;

namespace CircuitEditor.App.Interaction;

internal sealed class EditorLineCommands(TextEditor editor, CodeViewModel model)
{
    internal void Duplicate()
    {
        var (first, last) = SelectedLines();
        var document = editor.Document;
        var separator = last.DelimiterLength > 0
            ? document.GetText(last.EndOffset, last.DelimiterLength)
            : first.PreviousLine is { } previous
                ? document.GetText(previous.EndOffset, previous.DelimiterLength)
                : editor.Text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var body = document.GetText(first.Offset, last.EndOffset - first.Offset);
        if (!Fits(body.Length + separator.Length)) return;
        var selection = SelectionOffsets(first);
        document.Insert(last.EndOffset, separator + body);
        RestoreSelection(last.EndOffset + separator.Length, body.Length + last.DelimiterLength, selection);
    }

    internal void Move(int direction)
    {
        var document = editor.Document;
        var (first, last) = SelectedLines();
        var neighbor = direction < 0 ? first.PreviousLine : last.NextLine;
        if (neighbor is null) return;

        var selection = SelectionOffsets(first);
        var bodyLength = last.EndOffset - first.Offset;
        var body = document.GetText(first.Offset, bodyLength);
        var other = document.GetText(neighbor);
        var left = direction < 0 ? neighbor : last;
        var separator = document.GetText(left.EndOffset, left.DelimiterLength);
        var start = direction < 0 ? neighbor.Offset : first.Offset;
        var newOffset = direction < 0 ? start : start + other.Length + separator.Length;
        var trailing = direction < 0 ? separator.Length : neighbor.DelimiterLength;
        document.Replace(start, body.Length + separator.Length + other.Length,
            direction < 0 ? body + separator + other : other + separator + body);
        RestoreSelection(newOffset, bodyLength + trailing, selection);
    }

    internal void Delete()
    {
        var (first, last) = SelectedLines();
        var start = last.NextLine is null && first.PreviousLine is { } previous ? previous.EndOffset : first.Offset;
        editor.Document.Remove(start, last.EndOffset + last.DelimiterLength - start);
        RestoreSelection(start, 0, (0, 0));
    }

    internal void ToggleComment()
    {
        var document = editor.Document;
        var (first, last) = SelectedLines();
        var lines = Enumerable.Range(first.LineNumber, last.LineNumber - first.LineNumber + 1)
            .Select(document.GetLineByNumber).Select(line => (Line: line, Text: document.GetText(line)))
            .Where(item => !string.IsNullOrWhiteSpace(item.Text))
            .Select(item => (item.Line, Content: item.Text.TrimStart(), Indent: item.Text.Length - item.Text.TrimStart().Length))
            .ToArray();
        if (lines.Length == 0) return;
        var uncomment = lines.All(item => item.Content.StartsWith("//", StringComparison.Ordinal));
        if (!uncomment && !Fits(lines.Length * 3)) return;

        var caret = document.CreateAnchor(editor.CaretOffset);
        var start = document.CreateAnchor(editor.SelectionStart);
        var end = document.CreateAnchor(editor.SelectionStart + editor.SelectionLength);
        caret.SurviveDeletion = start.SurviveDeletion = end.SurviveDeletion = true;
        var hasSelection = editor.SelectionLength != 0;
        using (document.RunUpdate())
        {
            foreach (var (line, content, indent) in lines.Reverse())
            {
                if (uncomment) document.Remove(line.Offset + indent, content.StartsWith("// ", StringComparison.Ordinal) ? 3 : 2);
                else document.Insert(line.Offset + indent, "// ");
            }
        }
        if (hasSelection) editor.Select(start.Offset, end.Offset - start.Offset);
        else editor.Select(caret.Offset, 0);
    }

    private (DocumentLine First, DocumentLine Last) SelectedLines()
    {
        var document = editor.Document;
        var first = document.GetLineByOffset(editor.SelectionStart);
        var last = document.GetLineByOffset(editor.SelectionStart + Math.Max(editor.SelectionLength - 1, 0));
        return (first, last);
    }

    private (int Start, int End) SelectionOffsets(DocumentLine first)
    {
        var start = editor.SelectionLength == 0 ? editor.CaretOffset : editor.SelectionStart;
        return (start - first.Offset, start + editor.SelectionLength - first.Offset);
    }

    private void RestoreSelection(int offset, int length, (int Start, int End) selection)
    {
        var start = Math.Clamp(selection.Start, 0, length);
        var end = Math.Clamp(selection.End, start, length);
        editor.Select(offset + start, end - start);
        editor.TextArea.Caret.BringCaretToView();
    }

    private bool Fits(int added)
    {
        if ((long)editor.Document.TextLength + added <= SketchDocument.MaximumTextLength) return true;
        model.Output.Append("Текст превышает допустимый размер скетча.");
        return false;
    }
}
