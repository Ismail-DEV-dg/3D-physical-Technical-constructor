namespace CircuitEditor.Core.Code;
/// <summary>Достоверно распознанное использование пина в известном API.</summary>
public sealed record CodePinUse(BoardPinDefinition Pin, string Function, int Line, int Column, bool Writes);
