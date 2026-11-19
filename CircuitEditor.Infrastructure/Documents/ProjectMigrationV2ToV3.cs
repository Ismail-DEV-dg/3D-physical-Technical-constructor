using System.Text.Json.Nodes;
using CircuitEditor.Core.Documents;

namespace CircuitEditor.Infrastructure.Documents;

public sealed class ProjectMigrationV2ToV3 : IProjectMigration
{
    public int FromVersion => 2;
    public int ToVersion => 3;

    public JsonObject Migrate(JsonObject document)
    {
        document["simulationSetup"] = new JsonObject { ["instruments"] = new JsonObject() };
        document["schemaVersion"] = ToVersion;
        return document;
    }
}
