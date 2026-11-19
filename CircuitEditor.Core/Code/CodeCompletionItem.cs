namespace CircuitEditor.Core.Code;
/// <summary>Элемент API, его подсказка и известные аргументы-пины.</summary>
public sealed record CodeCompletionItem(string Text, string Description, string Category,
    IReadOnlyList<int> PinArguments, bool Writes = false, bool SetsMode = false);
