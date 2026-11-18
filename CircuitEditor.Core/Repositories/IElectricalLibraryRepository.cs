using CircuitEditor.Core.Electronics;
namespace CircuitEditor.Core.Repositories;
/// <summary>Хранилище электрических моделей, внутренних связей и профилей LED.</summary>
public interface IElectricalLibraryRepository
{
    /// <summary>Загружает проверенные данные.</summary>
    RepositoryResult<ElectricalCatalog> Load();
}
