namespace CircuitEditor.Core.Simulation;

/// <summary>Электрические параметры учебной модели семейства плат.</summary>
public sealed record SimulationBoardProfile(double LogicVoltage, double OutputResistanceOhms, double PullupResistanceOhms,
    int AdcBits, double RecommendedCurrentAmps, double AbsoluteCurrentAmps, long PwmPeriodMicroseconds);
