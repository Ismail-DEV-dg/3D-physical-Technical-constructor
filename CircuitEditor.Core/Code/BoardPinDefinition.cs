namespace CircuitEditor.Core.Code;
/// <summary>Таблица физических возможностей и обозначений пинов семейства платы.</summary>
public sealed record BoardPinDefinition(string Id, string Code, int Number, bool IsInputOnly, bool SupportsPwm,
    bool IsExposed, bool IsAnalog);
