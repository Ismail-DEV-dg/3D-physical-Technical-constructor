using System.Text.Json.Nodes;
using CircuitEditor.Core.Documents;
namespace CircuitEditor.Tests;
internal sealed class TestOldProjectMigration : IProjectMigration
{
    public int FromVersion => 0;
    public int ToVersion => 1;
    public JsonObject Migrate(JsonObject document) { document["libraryVersion"]="1"; return document; }
}
