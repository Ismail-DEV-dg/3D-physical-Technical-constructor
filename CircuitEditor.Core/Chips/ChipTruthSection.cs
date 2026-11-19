namespace CircuitEditor.Core.Chips;

/// <summary>Таблица секции; в каждой строке две строки символов: входы и выходы.</summary>
public sealed record ChipTruthSection(IReadOnlyList<int> Inputs, IReadOnlyList<int> Outputs,
    IReadOnlyList<IReadOnlyList<string>> Rows)
{
    /// <summary>Максимальное число входов секции.</summary>
    public const int MaximumInputs = 6;
    /// <summary>Максимальное число выходов секции.</summary>
    public const int MaximumOutputs = 8;
}
