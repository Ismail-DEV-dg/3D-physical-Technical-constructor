using System.IO;
using CircuitEditor.App.Services;

namespace CircuitEditor.Tests;

internal sealed class TestThemeResources : IThemeResources
{
    public List<ThemeKind> Applied { get; } = [];
    public bool FailOnApply { get; set; }

    public void Apply(ThemeKind theme)
    {
        if (FailOnApply)
            throw new IOException("Test resource failure.");
        Applied.Add(theme);
    }
}
