using System.Collections.Immutable;

namespace CircuitEditor.Core.Simulation;

/// <summary>Визуальные выходы и измеряемые значения учебной модели детали.</summary>
public sealed record ComponentSignal(double Intensity = 0, double Angle = 0, string? Text = null,
    ImmutableArray<double> Channels = default, int Segments = 0, bool Active = false,
    ImmutableDictionary<string, double>? Measurements = null);
