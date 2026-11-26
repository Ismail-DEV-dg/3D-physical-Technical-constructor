namespace CircuitEditor.Core.Models;

/// <summary>Текстовая подпись в физических координатах макета.</summary>
public sealed record MockupNote
{
    /// <summary>Максимальная длина подписи.</summary>
    public const int MaximumLength = 160;

    /// <summary>Создаёт подпись на холсте.</summary>
    public MockupNote(double x, double y, string text)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y) || string.IsNullOrWhiteSpace(text) ||
            text.Length > MaximumLength || text.Contains('\r') || text.Contains('\n'))
            throw new ArgumentException("Некорректная текстовая подпись на макете.");

        X = x;
        Y = y;
        Text = text;
    }

    /// <summary>Горизонтальная координата в миллиметрах.</summary>
    public double X { get; }
    /// <summary>Вертикальная координата в миллиметрах.</summary>
    public double Y { get; }
    /// <summary>Текст подписи.</summary>
    public string Text { get; }
}
