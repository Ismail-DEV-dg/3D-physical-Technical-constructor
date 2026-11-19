namespace CircuitEditor.Core.Simulation;

/// <summary>Типы учебных приборов макета.</summary>
public enum InstrumentKind
{
    /// <summary>Два зонда для измерения напряжения и тока.</summary>
    Meter,
    /// <summary>Осциллограф на два канала.</summary>
    Scope,
    /// <summary>Логический анализатор на восемь каналов.</summary>
    Logic
}
