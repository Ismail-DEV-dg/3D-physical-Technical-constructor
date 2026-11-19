namespace CircuitEditor.Core.Code;
/// <summary>Сопоставляет вывод детали с направлением Arduino API и суффиксом макроса.</summary>
public sealed record ComponentSignal(string Pin, string Suffix, string Direction);
