using CircuitEditor.Core.Models;

namespace CircuitEditor.Core.Layout;

/// <summary>Положение доступного вывода и отдельная внутренняя зона его подписи.</summary>
public sealed record PinLayout(Pin Pin, LayoutPoint Position, bool IsLeft, LayoutText Label);
