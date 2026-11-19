using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Documents;
using CircuitEditor.Core.Models;

namespace CircuitEditor.App.Services;

public sealed class ProjectChipVersionSelector(IUserChipLibraryService library, IDialogService dialogs) : IProjectChipVersionSelector
{
    public (CircuitProject Project, bool Updated) Resolve(ProjectDocument document, Func<ProjectDocument, CircuitProject> build)
    {
        var project = build(document);
        var updated = false;
        if (project.Chips.Count == 0) return (project, false);
        var warning = library.Refresh();
        if (warning is not null) dialogs.ShowWarning(warning);
        var current = document;
        foreach (var embedded in document.EmbeddedChips)
        {
            var local = library.Definitions.FirstOrDefault(chip => chip.Id == embedded.Id);
            if (local is null || ChipContent.Matches(embedded, local) ||
                !dialogs.UseLocalChipVersion(embedded.Name, embedded.DefinitionVersion, local.DefinitionVersion))
                continue;
            try
            {
                var candidate = ProjectChipDefinitions.Replace(current, embedded, local);
                if (ReferenceEquals(candidate, current)) continue;
                project = build(candidate);
                current = candidate;
                updated = true;
            }
            catch (Exception problem) when (problem is ArgumentException or InvalidOperationException or KeyNotFoundException)
            {
                dialogs.ShowWarning($"«{embedded.Name}»: использована встроенная ревизия. Обновление из библиотеки недоступно. {problem.Message}");
            }
        }
        return (project, updated);
    }
}
