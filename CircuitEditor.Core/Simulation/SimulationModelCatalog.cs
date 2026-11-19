namespace CircuitEditor.Core.Simulation;

/// <summary>Параметры плат и компонентов, загружаемые из библиотеки симуляции.</summary>
public sealed record SimulationModelCatalog(IReadOnlyDictionary<string, SimulationBoardProfile> Boards,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> Components);
