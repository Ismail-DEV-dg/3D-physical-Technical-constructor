using System.Text.Json.Serialization;

namespace CircuitEditor.Core.Placement;

/// <summary>Размещение в одном представлении: мм в макете, логические пиксели в схеме.</summary>
public sealed class ComponentPlacement
{
    /// <summary>Полный оборот в градусах.</summary>
    public const int FullRotation = 360;
    /// <summary>Разрешённый шаг поворота в градусах.</summary>
    public const int RotationStep = 90;
    /// <summary>Создаёт положение и угол; центр вращения определяется представлением.</summary>
    [JsonConstructor]
    public ComponentPlacement(double x = 0, double y = 0, int rotation = 0, int spanHoles = 0)
    {
        MoveTo(x, y);
        if (rotation % RotationStep != 0 || spanHoles < 0) throw new ArgumentException("Некорректное размещение.");
        Rotation = (rotation % FullRotation + FullRotation) % FullRotation; SpanHoles = spanHoles;
    }
    /// <summary>Координата X.</summary>
    [JsonInclude] public double X { get; private set; }
    /// <summary>Координата Y.</summary>
    [JsonInclude] public double Y { get; private set; }
    /// <summary>Угол в градусах.</summary>
    [JsonInclude] public int Rotation { get; private set; }
    /// <summary>Расстояние осевых выводов в шагах отверстий; 0 означает значение корпуса по умолчанию.</summary>
    public int SpanHoles { get; }
    /// <summary>Меняет только координаты этого представления.</summary>
    public void MoveTo(double x, double y)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y)) throw new ArgumentException("Неконечные координаты.");
        X = x; Y = y;
    }
    /// <summary>Поворачивает на четверть оборота.</summary>
    public void Rotate() => Rotation = (Rotation + RotationStep) % FullRotation;
    /// <summary>Независимая копия размещения.</summary>
    public ComponentPlacement Copy() => new(X, Y, Rotation, SpanHoles);
}
