namespace CircuitEditor.Core.Code;
/// <summary>Профиль платы и применяемых библиотек.</summary>
public sealed record CodeBoardProfile(BoardKind Kind, string ComponentId, string ServoHeader, IReadOnlyList<BoardPinDefinition> Pins,
    int? BuiltinLedNumber = null, int? DefaultSdaNumber = null, int? DefaultSclNumber = null);
