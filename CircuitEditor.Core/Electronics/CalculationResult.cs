namespace CircuitEditor.Core.Electronics;
/// <summary>Проверяемый результат формулы вместо исключения для пользовательского ввода.</summary>
public sealed record CalculationResult<T>(T? Value,string? Error) where T : class;
