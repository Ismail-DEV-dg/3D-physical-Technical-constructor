namespace CircuitEditor.Core.Repositories;

/// <summary>Хранилище обезличенной диагностики приложения.</summary>
public interface IErrorLogRepository
{
    /// <summary>Записывает тип исключения и методы без сообщения, входных данных и путей.</summary>
    bool TryAppend(Exception error);
}
