using System.Text.Json.Nodes;
using CircuitEditor.Core.Documents;

namespace CircuitEditor.Infrastructure.Documents;

public sealed class ProjectMigrationV3ToV4 : IProjectMigration
{
    public int FromVersion => 3;
    public int ToVersion => 4;

    public JsonObject Migrate(JsonObject document)
    {
        document["embeddedChips"] = new JsonArray();
        document["schemaVersion"] = ToVersion;
        return document;
    }
}
