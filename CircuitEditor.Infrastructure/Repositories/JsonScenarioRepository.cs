using CircuitEditor.Core.Repositories;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Infrastructure.Repositories;

public sealed class JsonScenarioRepository : IScenarioRepository
{
    public RepositoryResult<IReadOnlyList<ScenarioTemplate>> Load()
    {
        try
        {
            var templates = CodeJsonReader.Read<ScenarioTemplate[]>("scenarios.json");
            if (templates.Select(t => t.Id).Distinct().Count() != templates.Length ||
                templates.Any(t => string.IsNullOrWhiteSpace(t.Id) || string.IsNullOrWhiteSpace(t.Name)))
                return new(null, "Некорректные идентификаторы сценариев.");
            return new(templates, null);
        }
        catch (Exception error) when (JsonProjectRepository.IsDataError(error))
        {
            return new(null, error.Message);
        }
    }
}
