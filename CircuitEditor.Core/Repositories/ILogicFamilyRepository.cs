using CircuitEditor.Core.Chips;

namespace CircuitEditor.Core.Repositories;

/// <summary>Источник электрических профилей логических семейств.</summary>
public interface ILogicFamilyRepository
{
    /// <summary>Загружает профили с ключами, используемыми определениями микросхем.</summary>
    RepositoryResult<IReadOnlyDictionary<string, LogicFamilyProfile>> Load();
}
