using System.IO;
using System.Text.Json;
using CircuitEditor.Core.Documents;
using CircuitEditor.Core.History;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Wiring;
using CircuitEditor.Core.Models;
using CircuitEditor.Infrastructure.Repositories;
namespace CircuitEditor.Tests;
public sealed class ProjectFileTests
{
    [Fact]
    public void RoundTripUsesLibraryReferencesAndPreservesAllInstanceData()
    {
        using var e=new Stage4TestEnvironment();
        foreach(var d in e.Project.Library) e.Project.Add(d);
        var resistor=e.Project.Components.Single(c=>c.Model.Definition.Id=="resistor");
        var led=e.Project.Components.Single(c=>c.Model.Definition.Id=="led");
        var wire=new Wire(WireEndpoint.Contact(resistor.Model.Id,resistor.Footprint.Contacts[0].Id),
            WireEndpoint.Hole(e.Mockup.Board!.Id,e.Mockup.Board.Definition.Holes[0].Id),WirePalette.Colors[2],[new LayoutPoint(20,30)]);
        e.Project.Model.Circuit.PutWire(wire);
        var document=e.Session.Capture(); var file=Path.Combine(e.Folder,"test.circ");
        Assert.Null(e.Repository.Save(file,document));
        var json=File.ReadAllText(file); Assert.DoesNotContain("defaultColor",json); Assert.DoesNotContain("holePitch",json);
        Assert.DoesNotContain("electricalPinId",json);
        using var parsed=JsonDocument.Parse(json);
        Assert.Equal(1,parsed.RootElement.GetProperty("schemaVersion").GetInt32());
        var res=parsed.RootElement.GetProperty("components").EnumerateArray().Single(c=>c.GetProperty("definitionId").GetString()=="resistor");
        Assert.False(res.TryGetProperty("color",out _));
        var read=e.Repository.Load(file); Assert.True(read.Success,read.Error);
        var restored=ProjectDocumentResolver.Resolve(read.Value!,e.Project.Library,e.Project.Model.Footprints,e.Mockup.Board.Definition,new Core.Services.ComponentInsertionService());
        Assert.True(new ProjectSnapshot(e.Project.Model).Matches(restored));
        Assert.Equal(document.View,read.Value!.View);
        Assert.NotNull(led.Model.Color);
    }
    [Theory]
    [InlineData("{broken")]
    [InlineData("{\"schemaVersion\":99}")]
    [InlineData("{}")]
    public void DamagedAndFutureFilesNeverReplaceCurrentProject(string content)
    {
        using var e=new Stage4TestEnvironment(); e.Project.Add(e.Project.Library[0]);
        var before=new ProjectSnapshot(e.Project.Model); var file=Path.Combine(e.Folder,"bad.circ"); File.WriteAllText(file,content);
        Assert.False(e.Session.Load(file)); Assert.True(before.Matches(e.Project.Model)); Assert.Contains("открыть",e.Project.StatusMessage);
    }
    [Fact]
    public void MissingLibraryIdIsReportedBeforeSwappingProject()
    {
        using var e=new Stage4TestEnvironment(); e.Project.Add(e.Project.Library[0]);
        var before=new ProjectSnapshot(e.Project.Model); var document=e.Session.Capture();
        var bad=document with { Components=[document.Components[0] with {DefinitionId="missing-id"}] };
        var file=Path.Combine(e.Folder,"missing.circ"); Assert.Null(e.Repository.Save(file,bad));
        Assert.False(e.Session.Load(file)); Assert.Contains("missing-id",e.Project.StatusMessage); Assert.True(before.Matches(e.Project.Model));
    }
    [Fact]
    public void MigrationFrameworkUsesOnlyAnArtificialOldVersion()
    {
        using var e=new Stage4TestEnvironment(); var file=Path.Combine(e.Folder,"old.circ");
        Assert.Null(e.Repository.Save(file,e.Session.Capture() with {SchemaVersion=0,LibraryVersion="artificial"}));
        Assert.False(e.Repository.Load(file).Success);
        var migrated=new JsonProjectRepository([new TestOldProjectMigration()]).Load(file);
        Assert.True(migrated.Success,migrated.Error); Assert.Equal(1,migrated.Value!.SchemaVersion); Assert.Equal("1",migrated.Value.LibraryVersion);
    }
    [Fact]
    public void SaveCheckpointAndUnsavedGuardRespectCancelAndFailedSave()
    {
        using var e=new Stage4TestEnvironment(); e.Project.Add(e.Project.Library[0]);
        Assert.True(e.Project.IsDirty); Assert.False(e.Session.RequestClose());
        e.Target=Path.Combine(e.Folder,"saved.circ"); e.Session.SaveCommand.Execute(null);
        Assert.False(e.Project.IsDirty); Assert.DoesNotContain("*",e.Session.Title);
        e.Project.UndoCommand.Execute(null); Assert.True(e.Project.IsDirty);
        e.Project.RedoCommand.Execute(null); Assert.False(e.Project.IsDirty);
    }
}
