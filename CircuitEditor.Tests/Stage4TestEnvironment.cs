using System.IO;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Infrastructure.Repositories;
using CircuitEditor.Infrastructure.Documents;
namespace CircuitEditor.Tests;
internal sealed class Stage4TestEnvironment : IDialogService, IAutosaveService, IRecentProjectsRepository, IExportRepository, IDisposable
{
    public ProjectViewModel Project { get; } = TestProjectFactory.Create();
    public JsonProjectRepository Repository { get; }
    public BreadboardViewModel Mockup { get; }
    public SchematicViewModel Schematic { get; }
    public NavigationService Navigation { get; }
    public ProjectSessionViewModel Session { get; }
    public string Folder { get; } = Path.Combine(Path.GetTempPath(),"CircuitEditor-Tests",Guid.NewGuid().ToString("N"));
    public string? Target { get; set; }
    public SaveDecision Decision { get; set; } = SaveDecision.Cancel;
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];
    public void ShowError(string message) => Errors.Add(message);
    public void ShowWarning(string message) => Warnings.Add(message);
    public bool Recover { get; set; }
    public Action? Tick { get; private set; }
    public byte[]? Exported { get; private set; }
    public Stage4TestEnvironment()
    {
        Directory.CreateDirectory(Folder);
        Repository = new([new ProjectMigrationV1ToV2(), new ProjectMigrationV2ToV3()],Path.Combine(Folder,"recovery.circ"));
        Mockup=TestProjectFactory.Mockup(Project); Schematic=new(Project);
        Navigation=new(Mockup,Schematic,new ChipViewModel(),new ThreeDViewModel(),new CodeViewModel());
        Session=new(Project,Mockup,Schematic,Navigation,Repository,this,this,this,new JsonBreadboardRepository(),new Core.Services.ComponentInsertionService());
    }
    public string? OpenProject() => Target;
    public string? SaveFile(string filter,string extension,string? suggested=null) => Target;
    public SaveDecision ConfirmUnsaved() => Decision;
    public bool ConfirmRecovery() => Recover;
    public double? ChooseExportScale() => 1;
    public void Start(Action save) => Tick = save;
    public IReadOnlyList<string> Load() => [];
    public void Add(string path) { }
    public string? Write(string path,byte[] data) { Exported=data; return null; }
    public void Dispose() { Project.Dispose(); Mockup.Dispose(); Schematic.Dispose(); Directory.Delete(Folder,true); }
}
