namespace CircuitEditor.Core.Chips;

/// <summary>Чистое вычисление цифрового поведения; питание и задержку обеспечивает потребитель.</summary>
public interface IChipEvaluator
{
    /// <summary>Создаёт вычислитель для определения, прошедшего проверку.</summary>
    Func<IReadOnlyDictionary<int, ChipLevel>, ChipState, ChipEvaluation> Compile(ChipDefinition definition);
}
