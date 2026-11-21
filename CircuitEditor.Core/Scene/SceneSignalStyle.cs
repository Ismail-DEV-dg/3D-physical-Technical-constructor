namespace CircuitEditor.Core.Scene;

/// <summary>Общая шкала яркости и цвета напряжения для представлений симуляции.</summary>
public static class SceneSignalStyle
{
    /// <summary>Количество уровней яркости, включая погашенное состояние.</summary>
    public const int BrightnessLevels = 16;

    /// <summary>Квантует яркость в диапазоне от 0 до 15.</summary>
    public static int Brightness(double intensity) => double.IsFinite(intensity)
        ? (int)Math.Round(Math.Clamp(intensity, 0, 1) * (BrightnessLevels - 1)) : 0;

    /// <summary>Возвращает цвет провода для заданного напряжения.</summary>
    public static string VoltageColor(double voltage) => voltage switch
    {
        < -.1 => "#AF71FF",
        < .2 => "#72A8D0",
        < 2.4 => "#2FD4FF",
        < 4.3 => "#39FF88",
        _ => "#FF8D50"
    };
}
