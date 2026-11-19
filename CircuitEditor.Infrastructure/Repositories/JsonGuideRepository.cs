using CircuitEditor.Core.Diagnostics;
using CircuitEditor.Core.Repositories;

namespace CircuitEditor.Infrastructure.Repositories;

public sealed class JsonGuideRepository(IComponentLibraryRepository components) : IGuideRepository
{
    private const int ComponentVolumes = 7;

    public RepositoryResult<GuideLibrary> Load()
    {
        try
        {
            var navigation = CodeJsonReader.Read<GuideLibrary>("guides.json");
            var data = new GuideLibrary
            {
                Boards = navigation.Boards,
                Application = navigation.Application,
                Components = Enumerable.Range(1, ComponentVolumes)
                    .SelectMany(volume => CodeJsonReader.Read<GuideChapter[]>($"guides-components-{volume}.json")).ToArray()
            };
            var ids = components.Load().Components.Select(c => c.Id).ToHashSet();
            var chapterIds = data.Components.Where(c => c.Id != "start").Select(c => c.Id).ToArray();
            if (ids.Count == 0 || !ids.SetEquals(chapterIds) || chapterIds.Distinct().Count() != chapterIds.Length ||
                !new[] { "nano", "uno", "esp32" }.ToHashSet().SetEquals(data.Boards.Keys) ||
                data.Application.Count == 0 || data.Components.Any(c => string.IsNullOrWhiteSpace(c.Code)))
                throw new ArgumentException("Не хватает глав, примеров кода или описаний плат.");
            foreach (var board in data.Boards.Keys) data.Chapters(board);
            return new(data, null);
        }
        catch (Exception error) when (JsonProjectRepository.IsDataError(error) || error is KeyNotFoundException)
        {
            return new(null, "Не удалось открыть каталог пособий: " + error.Message);
        }
    }
}
