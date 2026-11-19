using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Repositories;

namespace CircuitEditor.Infrastructure.Repositories;

public sealed class JsonLogicFamilyRepository : ILogicFamilyRepository
{
    public RepositoryResult<IReadOnlyDictionary<string, LogicFamilyProfile>> Load()
    {
        try
        {
            var families = CodeJsonReader.Read<Dictionary<string, LogicFamilyProfile>>("logic-families.json");
            if (families.Count == 0 || families.Any(pair =>
                string.IsNullOrWhiteSpace(pair.Key) || pair.Value is not { IsValid: true }))
                throw new ArgumentException("Некорректные параметры логических семейств.");
            return new(new System.Collections.ObjectModel.ReadOnlyDictionary<string, LogicFamilyProfile>(families), null);
        }
        catch (Exception error) when (JsonProjectRepository.IsDataError(error))
        {
            return new(null, error.Message);
        }
    }
}
