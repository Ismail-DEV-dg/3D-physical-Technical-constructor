namespace CircuitEditor.Core.Physical;

/// <summary>Единое преобразование единиц и допуски физического макета.</summary>
public static class PhysicalSettings
{
    /// <summary>Исходное размещение единственной breadboard на столе, мм.</summary>
    public const double DefaultBoardX = 90;
    /// <summary>Верхнее поле breadboard, мм.</summary>
    public const double DefaultBoardY = 5;

    /// <summary>Пикселей WPF на мм при масштабе 100%.</summary>
    public const double PixelsPerMillimeter = 6;
    /// <summary>Допуск совмещения контакта и отверстия, мм.</summary>
    public const double HoleSnapToleranceMm = 0.4;
    /// <summary>Радиус захвата ближайшего отверстия при перетаскивании, мм.</summary>
    public const double HoleCaptureRadiusMm = 1.8;
    /// <summary>Шаг свободного размещения на столе, мм.</summary>
    public const double FreeGridMm = 2.54;
    /// <summary>Переводит мм в логические пиксели.</summary>
    public static double ToPixels(double mm) => mm * PixelsPerMillimeter;
    /// <summary>Переводит логические пиксели в мм.</summary>
    public static double ToMillimeters(double pixels) => pixels / PixelsPerMillimeter;
    /// <summary>Привязывает свободное размещение к общей сетке.</summary>
    public static double SnapFree(double mm) => Math.Round(mm / FreeGridMm) * FreeGridMm;
}
