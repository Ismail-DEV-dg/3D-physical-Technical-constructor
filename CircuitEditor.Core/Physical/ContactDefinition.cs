using CircuitEditor.Core.Layout;

namespace CircuitEditor.Core.Physical;

/// <summary>Физический контакт корпуса, связанный с электрическим выводом.</summary>
public sealed record ContactDefinition(string Id, string ElectricalPinId, double X, double Y)
{
    /// <summary>Локальная координата в мм.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public LayoutPoint Position => new(X, Y);
}
