namespace CircuitEditor.Core.Code;
/// <summary>Атомарная подготовка ручного скетча без изменения его инструкций.</summary>
public sealed record CodePreparationResult(bool Success,string Text,string Message);
