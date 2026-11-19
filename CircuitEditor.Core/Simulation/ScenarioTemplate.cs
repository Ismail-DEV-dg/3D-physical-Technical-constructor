namespace CircuitEditor.Core.Simulation;

/// <summary>Описание поддерживаемого сценария и его подписанных параметров.</summary>
public sealed record ScenarioTemplate(string Id, string Name, IReadOnlyDictionary<string, string> Pins,
    IReadOnlyDictionary<string, double> Numbers, string? Text = null, string? Target = null);
