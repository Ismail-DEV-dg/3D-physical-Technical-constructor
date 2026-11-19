using System.IO;
using System.Text.Json.Nodes;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Export;
using CircuitEditor.Core.History;
using CircuitEditor.Core.Models;
namespace CircuitEditor.Tests;
public sealed class FileNotificationTests : IImageExportService, ISvgExportService, IBomExportService
{
    private const string ExportError = "Не удалось записать экспортный файл.";
    public string? ExportPng(string path, WorkspaceKind kind, double scale) => ExportError;
    public string? ExportSchematic(string path) => ExportError;
    public byte[] Create(CircuitProject project) => [];
    public string? Export(string path, CircuitProject project) => ExportError;

    [Theory]
    [InlineData(99)]
    [InlineData(-1)]
    public void UnsupportedVersionDialogIncludesFileAndSupportedVersions(int version)
    {
        using var e = new Stage4TestEnvironment();
        e.Project.Add(e.Project.Library[0]);
        var before = new ProjectSnapshot(e.Project.Model);
        var path = Path.Combine(e.Folder, "version.circ");
        Assert.Null(e.Repository.Save(path, e.Session.Capture() with { SchemaVersion = version }));
        Assert.False(e.Session.Load(path));
        var message = Assert.Single(e.Errors);
        Assert.Contains($"Версия формата файла: {version}.", message);
        Assert.Contains($"Поддерживаемая версия: {CircuitEditor.Core.Documents.ProjectDocument.CurrentSchemaVersion}.", message);
        Assert.True(before.Matches(e.Project.Model));
    }

    [Fact]
    public void MissingLibraryVersionOpensWithWarningAndDoesNotModifyFile()
    {
        using var e = new Stage4TestEnvironment();
        e.Project.Add(e.Project.Library.Single(d => d.Id == "resistor"));
        var before = new ProjectSnapshot(e.Project.Model);
        var path = Path.Combine(e.Folder, "missing-library.circ");
        Assert.Null(e.Repository.Save(path, e.Session.Capture()));
        var json = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        json.Remove("libraryVersion"); var original = json.ToJsonString(); File.WriteAllText(path, original);
        Assert.True(e.Session.Load(path));
        Assert.Empty(e.Errors);
        Assert.Contains("libraryVersion", Assert.Single(e.Warnings));
        Assert.True(before.Matches(e.Project.Model));
        Assert.Equal(original, File.ReadAllText(path));
    }

    [Fact]
    public void MissingLibraryVersionDoesNotBypassUnknownIdValidation()
    {
        using var e = new Stage4TestEnvironment(); e.Project.Add(e.Project.Library[0]);
        var before = new ProjectSnapshot(e.Project.Model);
        var path = Path.Combine(e.Folder, "bad-id.circ");
        Assert.Null(e.Repository.Save(path, e.Session.Capture()));
        var json = JsonNode.Parse(File.ReadAllText(path))!.AsObject(); json.Remove("libraryVersion");
        json["components"]![0]!["definitionId"] = "unknown"; File.WriteAllText(path, json.ToJsonString());
        Assert.False(e.Session.Load(path)); Assert.Contains("unknown", Assert.Single(e.Errors));
        Assert.Empty(e.Warnings); Assert.True(before.Matches(e.Project.Model));
    }

    [Fact]
    public void DamagedFileAndFailedSaveHaveErrorDialogs()
    {
        using var e = new Stage4TestEnvironment();
        var path = Path.Combine(e.Folder, "broken.circ"); File.WriteAllText(path, "{broken");
        Assert.False(e.Session.Load(path)); Assert.Single(e.Errors);
        e.Target = e.Folder; e.Session.SaveCommand.Execute(null); Assert.Equal(2, e.Errors.Count);
    }

    [Theory]
    [InlineData("png")][InlineData("svg")][InlineData("bom")]
    public void ExportFailuresHaveErrorDialogs(string kind)
    {
        using var e = new Stage4TestEnvironment(); e.Target = "export";
        var vm = new ExportViewModel(e.Project, e.Navigation, e, this, this, this);
        if (kind == "png") vm.PngCommand.Execute(null);
        if (kind == "svg") { e.Navigation.Navigate(WorkspaceKind.Schematic); vm.SvgCommand.Execute(null); }
        if (kind == "bom") vm.BomCommand.Execute(null);
        Assert.Equal(ExportError, Assert.Single(e.Errors));
    }
}
