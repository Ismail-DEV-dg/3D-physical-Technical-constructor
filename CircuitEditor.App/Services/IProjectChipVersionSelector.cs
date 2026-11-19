using CircuitEditor.Core.Documents;
using CircuitEditor.Core.Models;

namespace CircuitEditor.App.Services;

public interface IProjectChipVersionSelector
{
    (CircuitProject Project, bool Updated) Resolve(ProjectDocument document, Func<ProjectDocument, CircuitProject> build);
}
