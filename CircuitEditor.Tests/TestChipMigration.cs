using System.Text.Json.Nodes;
using CircuitEditor.Core.Repositories;

namespace CircuitEditor.Tests;

internal sealed class TestChipMigration : IChipMigration
{
    public int FromVersion => 0;
    public int ToVersion => 1;

    public JsonObject Migrate(JsonObject source)
    {
        source["definition"] = source["legacyChip"]!.DeepClone();
        source.Remove("legacyChip");
        return source;
    }
}
