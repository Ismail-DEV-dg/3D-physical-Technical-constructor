namespace CircuitEditor.Core.Repositories;

/// <summary>Хранилище диагностических сообщений приложения.</summary>
public interface IErrorLogRepository
{
    /// <summary>Записывает сообщение без вторичного исключения при ошибке файловой системы.</summary>
    bool TryAppend(string message);
}
