using CircuitEditor.Core.Repositories;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Infrastructure.Repositories;

public sealed class JsonSimulationModelRepository : ISimulationModelRepository
{
    public RepositoryResult<SimulationModelCatalog> Load()
    {
        try
        {
            var catalog = CodeJsonReader.Read<SimulationModelCatalog>("simulation-models.json");
            if (catalog.Boards.Count != 3 || catalog.Boards.Values.Any(b =>
                b.AdcBits is not (10 or 12) || b.PwmPeriodMicroseconds <= 0 ||
                new[] { b.LogicVoltage, b.OutputResistanceOhms, b.PullupResistanceOhms,
                    b.RecommendedCurrentAmps, b.AbsoluteCurrentAmps }.Any(value => !double.IsFinite(value) || value <= 0)
                || b.AbsoluteCurrentAmps < b.RecommendedCurrentAmps))
                throw new ArgumentException("Некорректные параметры модели контроллера.");
            return new(catalog, null);
        }
        catch (Exception error) when (JsonProjectRepository.IsDataError(error))
        {
            return new(null, error.Message);
        }
    }
}
