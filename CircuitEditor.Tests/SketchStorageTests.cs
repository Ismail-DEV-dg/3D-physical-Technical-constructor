using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using CircuitEditor.Core.Code;
using CircuitEditor.Core.Documents;
using CircuitEditor.Infrastructure.Documents;
using CircuitEditor.Infrastructure.Repositories;
namespace CircuitEditor.Tests;
public sealed class SketchStorageTests
{
    [Fact]
    public void VersionOneFileMigratesToEmptySketchWithoutChangingCircuit()
    {
        using var e=new Stage4TestEnvironment();e.Project.Add(e.Project.Library[0]);
        var original=e.Session.Capture();
        var node=JsonSerializer.SerializeToNode(original,JsonProjectRepository.Options)!.AsObject();
        node["schemaVersion"]=1;node.Remove("sketch");node.Remove("simulationSetup");
        var path=Path.Combine(e.Folder,"v1.circ");File.WriteAllText(path,node.ToJsonString());
        var result=new JsonProjectRepository([new ProjectMigrationV1ToV2(),new ProjectMigrationV2ToV3()]).Load(path);
        Assert.True(result.Success,result.Error);Assert.Equal(ProjectDocument.CurrentSchemaVersion,result.Value!.SchemaVersion);
        Assert.Equal(new SketchDocument(),result.Value.Sketch);Assert.Equal(original.Components,result.Value.Components);
        Assert.Equal(original.Wires,result.Value.Wires);
    }
    [Fact]
    public void SketchRoundTripAutosaveAndCircuitUndoHaveIndependentHistories()
    {
        using var e=new Stage4TestEnvironment();var p=e.Project;
        var sketch=new SketchDocument("void setup() {}",BoardKind.Esp32,"Demo");
        p.UpdateSketch(sketch);Assert.True(p.IsDirty);Assert.Equal(0,p.History.Count);
        p.Add(p.Library[0]);p.UndoCommand.Execute(null);Assert.Equal(sketch,p.Model.Sketch);
        p.RedoCommand.Execute(null);Assert.Equal(sketch,p.Model.Sketch);
        e.Target=Path.Combine(e.Folder,"sketch.circ");e.Session.SaveCommand.Execute(null);Assert.False(p.IsDirty);
        p.UpdateSketch(sketch with {Text="manual edit"});Assert.True(p.IsDirty);
        p.UpdateSketch(sketch);Assert.False(p.IsDirty);
        p.UpdateSketch(sketch with {Text="recovery"});e.Session.Autosave();
        Assert.Equal("recovery",e.Repository.Load(e.Repository.RecoveryPath).Value!.Sketch.Text);
        Assert.True(e.Session.Load(e.Target));Assert.Equal(sketch,p.Model.Sketch);Assert.False(p.IsDirty);
    }
    [Fact]
    public void ExportCreatesMatchingArduinoFolderAndRejectsInvalidName()
    {
        using var e=new Stage4TestEnvironment();var repo=new FileSketchRepository();
        var result=repo.Export(Path.Combine(e.Folder,"Demo.ino"),"// привет");
        Assert.True(result.Success,result.Error);Assert.Equal(Path.Combine(e.Folder,"Demo","Demo.ino"),result.Value);
        Assert.Equal("// привет",File.ReadAllText(result.Value!));
        Assert.Equal(result.Value,repo.Export(result.Value!,"updated").Value);
        Assert.False(repo.Export(Path.Combine(e.Folder,"Bad Name.ino"),"text").Success);
    }
    [Theory]
    [InlineData("123_Demo-v2.0",true)]
    [InlineData("_Demo",true)]
    [InlineData("Demo.",false)]
    [InlineData(".Demo",false)]
    [InlineData("CON",false)]
    [InlineData("com1.test",false)]
    public void ArduinoNamesFollowFolderAndPrimaryFileRules(string name,bool valid)=>Assert.Equal(valid,SketchName.IsValid(name));
}
