using CircuitEditor.Core.Models;

namespace CircuitEditor.Core.Repositories;

/// <summary>Результат атомарной загрузки библиотеки без необработанных ошибок пользовательских данных.</summary>
public sealed record LibraryLoadResult(IReadOnlyList<ComponentDefinition> Components, string? Error)
{
    /// <summary>Признак успешной загрузки.</summary>
    public bool Success => Error is null;
}
