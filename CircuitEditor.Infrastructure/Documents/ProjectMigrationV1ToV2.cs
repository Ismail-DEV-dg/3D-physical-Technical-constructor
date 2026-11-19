using System.Text.Json.Nodes;
using CircuitEditor.Core.Documents;
namespace CircuitEditor.Infrastructure.Documents;
public sealed class ProjectMigrationV1ToV2 : IProjectMigration
{
    public int FromVersion => 1;
    public int ToVersion => 2;
    public JsonObject Migrate(JsonObject document)
    {
        document["sketch"] = new JsonObject { ["text"] = "", ["board"] = "Uno", ["name"] = "Sketch" };
        document["schemaVersion"] = ToVersion;
        return document;
    }
}
