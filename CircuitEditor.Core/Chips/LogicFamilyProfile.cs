namespace CircuitEditor.Core.Chips;

/// <summary>Номиналы учебной цифровой модели логического семейства.</summary>
public sealed record LogicFamilyProfile(string Name, double NominalSupply,
    double MinimumSupply, double MaximumSupply, LogicVoltage InputLow, LogicVoltage InputHigh,
    LogicVoltage OutputLow, LogicVoltage OutputHigh, double SourceResistanceOhms, double SinkResistanceOhms,
    double SourceLimitAmps, double SinkLimitAmps)
{
    /// <summary>Профиль имеет упорядоченные уровни и положительные диапазоны.</summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(Name)
        && new[] { NominalSupply, MinimumSupply, MaximumSupply, SourceResistanceOhms, SinkResistanceOhms,
            SourceLimitAmps, SinkLimitAmps }.All(value => double.IsFinite(value) && value > 0)
        && MinimumSupply <= NominalSupply && NominalSupply <= MaximumSupply
        && new[] { InputLow, InputHigh, OutputLow, OutputHigh }.All(level => level is { IsValid: true })
        && new[] { MinimumSupply, MaximumSupply }.All(supply =>
            InputLow.Resolve(supply) < InputHigh.Resolve(supply)
            && InputHigh.Resolve(supply) <= supply
            && OutputLow.Resolve(supply) < OutputHigh.Resolve(supply)
            && OutputHigh.Resolve(supply) <= supply);

    /// <summary>Преобразует измеренное напряжение относительно GND в логический уровень.</summary>
    public ChipLevel Read(double? voltage, double supply) => voltage switch
    {
        null => ChipLevel.Z,
        var v when v <= InputLow.Resolve(supply) => ChipLevel.Low,
        var v when v >= InputHigh.Resolve(supply) => ChipLevel.High,
        _ => ChipLevel.X
    };

    /// <summary>При положительном токе ток втекает в вывод, при отрицательном вытекает.</summary>
    public bool ExceedsCurrentLimit(double amps) => amps > SinkLimitAmps || amps < -SourceLimitAmps;
}
