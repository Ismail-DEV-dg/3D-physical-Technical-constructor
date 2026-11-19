namespace CircuitEditor.Core.Code;
/// <summary>Лексема без комментариев/литералов с исходной позицией.</summary>
public sealed record CppToken(string Text, int Offset, int Line, int Column);
