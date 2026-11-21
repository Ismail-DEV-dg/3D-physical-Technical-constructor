using CircuitEditor.Core.Repositories;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.Infrastructure.Repositories;

public sealed class JsonSceneCatalogRepository : ISceneCatalogRepository
{
    public RepositoryResult<SceneCatalog> Load()
    {
        try
        {
            var data = CodeJsonReader.Read<SceneCatalog>("scene-profiles.json");
            if (data.Profiles.Count == 0 || data.Profiles.Values.Append(data.Breadboard).Append(data.Fallback).Any(profile =>
                profile is null || !Enum.IsDefined(profile.Shape) || !FinitePositive(profile.Height) ||
                !double.IsFinite(profile.Clearance) || profile.Clearance < 0 || !double.IsFinite(profile.DetailHeight) ||
                profile.DetailHeight < 0 || !CircuitEditor.Core.Models.ComponentProperties.IsColor(profile.Material)) ||
                !FinitePositive(data.Wire.Radius) || !FinitePositive(data.Wire.MinimumRise) ||
                !FinitePositive(data.Wire.MaximumRise) || data.Wire.MaximumRise < data.Wire.MinimumRise ||
                !FinitePositive(data.Wire.RisePerMillimeter))
                throw new ArgumentException("Неверные параметры 3D-корпусов.");
            return new(data, null);
        }
        catch (Exception error) when (JsonProjectRepository.IsDataError(error))
        {
            return new(null, error.Message);
        }
    }

    private static bool FinitePositive(double value) => double.IsFinite(value) && value > 0;
}
