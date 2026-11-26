using CircuitEditor.Core.Repositories;

namespace CircuitEditor.Tests;

internal sealed class TestShortcutSettingsRepository : IShortcutSettingsRepository
{
    private IReadOnlyDictionary<string, string> overrides = new Dictionary<string, string>();
    internal string? Error { get; set; }
    public IReadOnlyDictionary<string, string> Load() => overrides;
    public string? Save(IReadOnlyDictionary<string, string> values)
    {
        if (Error is null) overrides = new Dictionary<string, string>(values);
        return Error;
    }
}
