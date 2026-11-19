namespace CircuitEditor.Core.Chips;

/// <summary>Уровень в вольтах либо в долях действительного напряжения питания.</summary>
public sealed record LogicVoltage(double Value, bool SupplyFraction = false)
{
    /// <summary>Порог или уровень при заданном питании.</summary>
    public double Resolve(double supply) => SupplyFraction ? Value * supply : Value;

    /// <summary>Уровень задан действительным неотрицательным числом.</summary>
    public bool IsValid => double.IsFinite(Value) && Value >= 0 && (!SupplyFraction || Value <= 1);
}
