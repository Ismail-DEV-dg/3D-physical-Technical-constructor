namespace CircuitEditor.Core.Models;

/// <summary>Ссылка на вывод конкретного экземпляра компонента.</summary>
public sealed record PinReference(Guid ComponentId, string PinId);
