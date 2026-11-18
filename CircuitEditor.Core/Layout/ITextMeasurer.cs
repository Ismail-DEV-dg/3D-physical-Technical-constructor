namespace CircuitEditor.Core.Layout;

/// <summary>Платформенный измеритель текста; Core не знает о шрифтах WPF.</summary>
public interface ITextMeasurer
{
    /// <summary>Измеряет полный текст без сжатия, переноса по ширине и обрезки.</summary>
    TextExtent Measure(string text, double fontSize);
}
