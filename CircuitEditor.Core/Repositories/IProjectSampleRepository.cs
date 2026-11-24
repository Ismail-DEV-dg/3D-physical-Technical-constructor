using CircuitEditor.Core.Documents;

namespace CircuitEditor.Core.Repositories;

/// <summary>Каталог встроенных проектов и защита его файлов от перезаписи.</summary>
public interface IProjectSampleRepository
{
    /// <summary>Читает список примеров с описаниями.</summary>
    IReadOnlyList<ProjectSample> Load();
    /// <summary>Возвращает путь к примеру из каталога.</summary>
    string PathFor(ProjectSample sample);
    /// <summary>Проверяет, входит ли файл в каталог примеров.</summary>
    bool IsOriginal(string path);
}
