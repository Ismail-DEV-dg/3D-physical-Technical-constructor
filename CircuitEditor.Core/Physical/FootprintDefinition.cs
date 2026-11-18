using CircuitEditor.Core.Layout;

namespace CircuitEditor.Core.Physical;

/// <summary>Данные физического корпуса; не зависят от длины подписей или WPF.</summary>
public sealed record FootprintDefinition(
    string Id, string ComponentDefinitionId, string Renderer, string Mounting,
    LayoutRect Body, IReadOnlyList<ContactDefinition> Contacts, IReadOnlyList<int> AllowedRotations,
    int? MinSpan = null, int? MaxSpan = null, int DefaultSpan = 0, double TolerancePercent = 5,
    IReadOnlyDictionary<string, LayoutRect>? Parts = null, RotaryControlDefinition? RotaryControl = null)
{
    /// <summary>Корпус допускает изменение расстояния между осевыми ножками.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsFlexible => MinSpan.HasValue && MaxSpan.HasValue;
}
