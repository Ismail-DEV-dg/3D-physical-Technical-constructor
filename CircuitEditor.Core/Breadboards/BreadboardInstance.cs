namespace CircuitEditor.Core.Breadboards;

/// <summary>Экземпляр breadboard в физическом макете; в схеме не размещается.</summary>
public sealed record BreadboardInstance(Guid Id, BreadboardDefinition Definition, double X, double Y);
