namespace CircuitEditor.Core.Scene;

/// <summary>Размеры посадочных отверстий и выводов в трёхмерном представлении, в миллиметрах.</summary>
public static class SceneContactDepth
{
    /// <summary>Глубина видимой шахты в макетной плате.</summary>
    public const double BreadboardBore = 1.65;

    /// <summary>Заход ножки или перемычки в отверстие.</summary>
    public const double Insertion = 1.22;

    /// <summary>Высота гнезда Arduino Uno над текстолитом.</summary>
    public const double UnoSocketHeight = 6.1;

    /// <summary>Глубина шахты гнезда Arduino Uno.</summary>
    public const double UnoSocketBore = 1.5;

    /// <summary>Высота видимого штыря Nano или ESP32 над текстолитом.</summary>
    public const double HeaderPost = 1.65;
}
