namespace CircuitEditor.Core.Layout;

/// <summary>Прямоугольник раскладки без зависимости от графической платформы.</summary>
public readonly record struct LayoutRect(double X, double Y, double Width, double Height)
{
    /// <summary>Правая граница.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public double Right => X + Width;
    /// <summary>Нижняя граница.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public double Bottom => Y + Height;
    /// <summary>Проверяет положительную площадь пересечения; касание не считается пересечением.</summary>
    public bool Intersects(LayoutRect other) =>
        X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;
    /// <summary>Проверяет полное включение другого прямоугольника.</summary>
    public bool Contains(LayoutRect other) =>
        other.X >= X && other.Y >= Y && other.Right <= Right && other.Bottom <= Bottom;
    /// <summary>Проверяет включение точки.</summary>
    public bool Contains(LayoutPoint point) =>
        point.X >= X && point.X <= Right && point.Y >= Y && point.Y <= Bottom;
}
