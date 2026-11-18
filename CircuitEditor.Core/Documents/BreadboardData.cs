namespace CircuitEditor.Core.Documents;
/// <summary>Размещение платы без встраивания её топологии.</summary>
public sealed record BreadboardData(Guid Id, string DefinitionId, double X, double Y, int Rotation = 0);
