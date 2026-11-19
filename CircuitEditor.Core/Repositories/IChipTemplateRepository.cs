using CircuitEditor.Core.Chips;

namespace CircuitEditor.Core.Repositories;

/// <summary>Источник встроенных редактируемых примеров микросхем.</summary>
public interface IChipTemplateRepository
{
    /// <summary>Возвращает проверенный набор встроенных определений.</summary>
    RepositoryResult<IReadOnlyList<ChipDefinition>> Load();
}
