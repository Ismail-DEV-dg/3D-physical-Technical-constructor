namespace CircuitEditor.Core.Code;
/// <summary>Диагностика кода с позицией, пригодной для перехода в редакторе.</summary>
public sealed record CodeProblem(string Message, int Line = 1, int Column = 1, string Severity = "Warning",string Code="GENERAL");
