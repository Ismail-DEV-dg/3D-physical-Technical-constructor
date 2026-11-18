namespace CircuitEditor.Core.Layout;

/// <summary>Полный текст и зарезервированный для него прямоугольник.</summary>
public sealed record LayoutText(string Text, LayoutRect Bounds, double FontSize);
