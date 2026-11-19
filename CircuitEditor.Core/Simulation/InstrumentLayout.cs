using System.Text.Json.Serialization;

namespace CircuitEditor.Core.Simulation;

/// <summary>Положение и видимость панели прибора в логических пикселях макета.</summary>
public sealed record InstrumentLayout(double X, double Y, bool Visible)
{
    /// <summary>Ограничивает координаты некорректных файлов.</summary>
    public const double MaximumCoordinate = 100_000;

    /// <summary>Проверяет конечные и неотрицательные координаты.</summary>
    [JsonIgnore]
    public bool IsValid => double.IsFinite(X) && double.IsFinite(Y) &&
        X is >= 0 and <= MaximumCoordinate && Y is >= 0 and <= MaximumCoordinate;
}
