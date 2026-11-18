namespace CircuitEditor.App.Interaction;

public sealed record CanvasInput(CanvasAction Action, double X = 0, double Y = 0,
    int Delta = 0, bool Control = false, bool Pan = false, string? DefinitionId = null);
