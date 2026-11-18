namespace CircuitEditor.Core.Layout;

/// <summary>Готовая раскладка: корпус, символ, текстовые зоны и уплотнённые ряды выводов.</summary>
public sealed record ComponentLayout(double Width, double Height, LayoutRect Body, LayoutRect Symbol,
    IReadOnlyList<LayoutText> Texts, IReadOnlyList<PinLayout> Pins, double PinPitch)
{
    /// <summary>Все подписи, включая заголовки и номинал.</summary>
    public IEnumerable<LayoutText> AllLabels => Texts.Concat(Pins.Select(pin => pin.Label));
}
