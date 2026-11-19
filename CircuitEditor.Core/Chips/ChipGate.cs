namespace CircuitEditor.Core.Chips;

/// <summary>Блок с упорядоченными портами; у D и JK возможны асинхронные входы сброса и установки.</summary>
public sealed record ChipGate(int Id, ChipGateKind Kind, IReadOnlyList<ChipSignal> Inputs,
    ChipSignal? Clear = null, ChipSignal? Preset = null)
{
    /// <summary>Блок хранит состояние между вычислениями.</summary>
    public bool HasMemory => Kind is ChipGateKind.D or ChipGateKind.Jk or ChipGateKind.Rs;
}
