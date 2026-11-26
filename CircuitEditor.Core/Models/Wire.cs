using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Wiring;
using System.Text.Json.Serialization;
namespace CircuitEditor.Core.Models;
/// <summary>Перемычка: связность задаётся исключительно устойчивыми концами.</summary>
public sealed class Wire
{
    /// <summary>Создаёт физическую перемычку.</summary>
    public Wire(WireEndpoint start, WireEndpoint end, string color = "#FF3B5C", IReadOnlyList<LayoutPoint>? controlPoints = null, bool isStraight = false)
        : this(Guid.NewGuid(), start, end, color, controlPoints, isStraight) { }
    /// <summary>Восстанавливает перемычку.</summary>
    [JsonConstructor]
    public Wire(Guid id, WireEndpoint start, WireEndpoint end, string color = "#FF3B5C", IReadOnlyList<LayoutPoint>? controlPoints = null, bool isStraight = false)
    {
        if (id == Guid.Empty || start is null || end is null || start == end || start.OwnerId == Guid.Empty || end.OwnerId == Guid.Empty
            || string.IsNullOrWhiteSpace(start.TerminalId) || string.IsNullOrWhiteSpace(end.TerminalId)
            || !Enum.IsDefined(start.Kind) || !Enum.IsDefined(end.Kind) || !WirePalette.Colors.Contains(color))
            throw new ArgumentException("Некорректная перемычка или цвет.");
        var points = controlPoints?.ToArray() ?? [];
        if (points.Any(p => !double.IsFinite(p.X) || !double.IsFinite(p.Y))) throw new ArgumentException("Некорректный изгиб.");
        Id = id; Start = start; End = end; Color = color; ControlPoints = Array.AsReadOnly(points); IsStraight = isStraight;
    }
    /// <summary>Совместимость старых внутренних символьных тестов; файлы используют физические ID.</summary>
    public Wire(PinReference start, PinReference end) : this(WireEndpoint.Contact(start.ComponentId, start.PinId), WireEndpoint.Contact(end.ComponentId, end.PinId)) { }
    /// <summary>Устойчивый ID.</summary>
    public Guid Id { get; }
    /// <summary>Первый конец.</summary>
    public WireEndpoint Start { get; }
    /// <summary>Второй конец.</summary>
    public WireEndpoint End { get; }
    /// <summary>Цвет из палитры.</summary>
    public string Color { get; }
    /// <summary>Управляющие узлы в мм; на электрическую связность не влияют.</summary>
    public IReadOnlyList<LayoutPoint> ControlPoints { get; }
    /// <summary>Рисовать управляющие участки прямыми отрезками.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsStraight { get; }
}
