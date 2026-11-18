namespace CircuitEditor.Core.Breadboards;

/// <summary>Вставка физического контакта компонента в отверстие конкретной платы.</summary>
public sealed record ContactBinding(Guid ComponentId, string ContactId, Guid BoardId, string HoleId);
