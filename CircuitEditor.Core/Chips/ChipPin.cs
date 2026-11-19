namespace CircuitEditor.Core.Chips;

/// <summary>Вывод DIP с постоянным номером и опциональным управлением состоянием Z.</summary>
public sealed record ChipPin(int Number, string Name, ChipPinType Type, string Description = "",
    bool IsUnused = false, int? EnablePin = null, bool EnableLow = true)
{
    /// <summary>Вывод принимает логический сигнал.</summary>
    public bool Reads => !IsUnused && Type is ChipPinType.Input or ChipPinType.Bidirectional;
    /// <summary>Вывод управляет логическим сигналом.</summary>
    public bool Drives => Type is ChipPinType.Output or ChipPinType.Bidirectional or ChipPinType.OpenCollector;
}
