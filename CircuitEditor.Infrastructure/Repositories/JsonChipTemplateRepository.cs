using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Repositories;

namespace CircuitEditor.Infrastructure.Repositories;

public sealed class JsonChipTemplateRepository(ILogicFamilyRepository profiles) : IChipTemplateRepository
{
    private const string DataPrefix = "CircuitEditor.Infrastructure.Data.";
    public RepositoryResult<IReadOnlyList<ChipDefinition>> Load()
    {
        var families = profiles.Load();
        if (families.Value is null) return new(null, families.Error);
        try
        {
            var templates = typeof(JsonChipTemplateRepository).Assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(DataPrefix + "Chips.", StringComparison.Ordinal)
                    && name.EndsWith(".json", StringComparison.Ordinal))
                .Select(name => CodeJsonReader.Read<ChipDefinition>(name[DataPrefix.Length..])).ToArray();
            if (templates.Length == 0 || templates.Select(template => template.Id).Distinct().Count() != templates.Length)
                throw new ArgumentException("Нет шаблонов микросхем либо их ID повторяются.");
            foreach (var template in templates)
            {
                var errors = ChipValidator.Validate(template, families.Value);
                if (errors.Count > 0) throw new ArgumentException($"{template.Name}: {errors[0]}");
            }
            return new(templates, null);
        }
        catch (Exception error) when (JsonProjectRepository.IsDataError(error))
        {
            return new(null, "Библиотека шаблонов: " + error.Message);
        }
    }
}
