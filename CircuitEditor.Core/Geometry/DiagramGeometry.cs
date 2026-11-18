using CircuitEditor.Core.Placement;

namespace CircuitEditor.Core.Geometry;

/// <summary>Операции над размещением условного символа без изменения электрического компонента.</summary>
public static class DiagramGeometry
{
    /// <summary>Шаг сетки условной схемы.</summary>
    public const double GridSize = 20;
    /// <summary>Привязывает координату к сетке.</summary>
    public static double Snap(double value) => Math.Round(value / GridSize, MidpointRounding.AwayFromZero) * GridSize;
    /// <summary>Проверяет попадание в повёрнутые фактические границы.</summary>
    public static bool Contains(ComponentPlacement placement, double x, double y, double width, double height)
    {
        var cx = placement.X + width / 2; var cy = placement.Y + height / 2;
        var angle = -placement.Rotation * Math.PI / 180;
        var localX = (x - cx) * Math.Cos(angle) - (y - cy) * Math.Sin(angle);
        var localY = (x - cx) * Math.Sin(angle) + (y - cy) * Math.Cos(angle);
        return Math.Abs(localX) <= width / 2 && Math.Abs(localY) <= height / 2;
    }
    /// <summary>Пересечение рамки с ограничивающим прямоугольником.</summary>
    public static bool Intersects(ComponentPlacement placement, double left, double top, double right, double bottom,
        double width, double height)
    {
        var rotated = placement.Rotation % 180 != 0;
        var cx = placement.X + width / 2; var cy = placement.Y + height / 2;
        var boundingWidth = rotated ? height : width; var boundingHeight = rotated ? width : height;
        return cx + boundingWidth / 2 >= left && cx - boundingWidth / 2 <= right
            && cy + boundingHeight / 2 >= top && cy - boundingHeight / 2 <= bottom;
    }
}
