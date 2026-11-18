namespace CircuitEditor.Core.Repositories;

/// <summary>Источник библиотечных описаний компонентов.</summary>
public interface IComponentLibraryRepository
{
    /// <summary>Загружает библиотеку или возвращает диагностическую ошибку.</summary>
    LibraryLoadResult Load();
}
