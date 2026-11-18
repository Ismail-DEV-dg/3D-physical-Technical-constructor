using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.Documents;
/// <summary>Экземпляр со ссылками на библиотеку и только применимыми параметрами.</summary>
public sealed record ComponentData(Guid Id, string DefinitionId, string FootprintId, string Reference,
    string Value, string? Color = null, string? I2cAddress = null, CapacitorKind? CapacitorKind = null,
    double? PotentiometerAngle = null, ComponentInputs? Inputs = null);
