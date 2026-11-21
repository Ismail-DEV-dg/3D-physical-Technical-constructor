namespace CircuitEditor.Core.Scene;

/// <summary>Конец физического контакта в локальной системе узла, с устойчивым ID отверстия при монтаже.</summary>
public sealed record SceneContact(string Id, ScenePoint Position, string? HoleId = null, Guid? BoardId = null);
