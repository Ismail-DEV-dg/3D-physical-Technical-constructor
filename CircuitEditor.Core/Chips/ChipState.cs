namespace CircuitEditor.Core.Chips;

/// <summary>Снимок памяти одного экземпляра цифровой микросхемы.</summary>
public sealed record ChipState(IReadOnlyDictionary<int, ChipLevel> Memory, IReadOnlyDictionary<int, ChipLevel> Clocks)
{
    /// <summary>Сброшенное состояние без установленного уровня памяти.</summary>
    public static ChipState Empty { get; } = new(EmptyLevels(), EmptyLevels());

    private static IReadOnlyDictionary<int, ChipLevel> EmptyLevels() =>
        new System.Collections.ObjectModel.ReadOnlyDictionary<int, ChipLevel>(new Dictionary<int, ChipLevel>());
}
