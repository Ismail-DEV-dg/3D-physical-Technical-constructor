using System.Collections.Immutable;
using CircuitEditor.Core.Models;

namespace CircuitEditor.Core.Simulation;

/// <summary>Выбранный сценарий, стабильные привязки и пользовательские параметры.</summary>
public sealed record ScenarioConfiguration(string TemplateId, ImmutableDictionary<string, PinReference> Pins,
    ImmutableDictionary<string, double> Numbers, Guid? TargetId = null, string? Text = null);
