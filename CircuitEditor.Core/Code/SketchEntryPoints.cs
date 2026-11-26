namespace CircuitEditor.Core.Code;

internal static class SketchEntryPoints
{
    private const string EndOfDraft = "__CIRCUIT_EDITOR_END_OF_DRAFT__";

    internal static string Ensure(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return SketchDocument.StarterText;

        var tokens = CppTokenScanner.Scan(text + "\n\n" + EndOfDraft);
        if (tokens.Count == 0 || tokens[^1].Text != EndOfDraft) return text;

        var depth = 0;
        var hasSetup = false;
        var hasLoop = false;
        for (var index = 0; index < tokens.Count - 1; index++)
        {
            var token = tokens[index].Text;
            if (depth == 0 && token == "void" && index + 4 < tokens.Count &&
                tokens[index + 1].Text is "setup" or "loop" && tokens[index + 2].Text == "(")
            {
                var closing = index + (tokens[index + 3].Text == "void" ? 4 : 3);
                if (closing + 1 < tokens.Count && tokens[closing].Text == ")" && tokens[closing + 1].Text == "{")
                {
                    hasSetup |= tokens[index + 1].Text == "setup";
                    hasLoop |= tokens[index + 1].Text == "loop";
                }
            }
            if (token == "{") depth++;
            else if (token == "}" && --depth < 0) return text;
        }
        if (depth != 0 || hasSetup && hasLoop) return text;

        var newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var result = text;
        if (!hasSetup) result += $"{newline}{newline}void setup() {{{newline}{newline}}}{newline}";
        if (!hasLoop) result += $"{newline}{newline}void loop() {{{newline}{newline}}}{newline}";
        return result.Length <= SketchDocument.MaximumTextLength ? result : text;
    }
}
