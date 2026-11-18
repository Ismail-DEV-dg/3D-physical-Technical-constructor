using CircuitEditor.Core.Layout;

namespace CircuitEditor.Core.Breadboards;

/// <summary>Отверстие с устойчивым ID и локальной координатой в мм.</summary>
public sealed record BreadboardHole(string Id, LayoutPoint Position, string GroupId);
