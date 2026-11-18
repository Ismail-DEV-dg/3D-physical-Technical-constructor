namespace CircuitEditor.Core.Repositories;

/// <summary>Результат чтения данных с диагностикой вместо исключения в UI.</summary>
public sealed record RepositoryResult<T>(T? Value, string? Error)
{
    /// <summary>Некритичное предупреждение при успешной загрузке.</summary>
    public string? Warning { get; init; }
    /// <summary>Данные успешно загружены.</summary>
    public bool Success => Error is null && Value is not null;
}
