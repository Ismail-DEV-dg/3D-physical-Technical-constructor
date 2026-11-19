namespace CircuitEditor.Core.Code;
/// <summary>Ограниченный шаблон компонента, загружаемый из данных.</summary>
public sealed record CodeTemplate(string ComponentId, IReadOnlyList<ComponentSignal> Signals,
    IReadOnlyList<string> Includes, string Declarations, string Setup, string Loop,
    bool RequiresPwm = false, bool IsI2c = false, string Notes = "", string StandaloneLoop = "");
