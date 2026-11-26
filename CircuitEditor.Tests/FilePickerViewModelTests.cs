using System.IO;
using CircuitEditor.App.ViewModels;

namespace CircuitEditor.Tests;

public sealed class FilePickerViewModelTests
{
    [Fact]
    public void OpenFiltersFilesAndAcceptsTypedPath()
    {
        using var environment = new Stage4TestEnvironment();
        var circuit = Path.Combine(environment.Folder, "пример.circ");
        File.WriteAllText(circuit, "{}");
        File.WriteAllText(Path.Combine(environment.Folder, "notes.txt"), "");
        Directory.CreateDirectory(Path.Combine(environment.Folder, "Samples"));
        string? accepted = null;
        var model = new FilePickerViewModel("Проекты|*.circ", null, null, environment.Folder,
            path => accepted = path, _ => false);

        Assert.Equal(new[] { "Samples", "пример.circ" }, model.Entries.Select(entry => entry.Name));
        model.FileName = "missing.circ";
        model.AcceptCommand.Execute(null);
        Assert.Null(accepted);
        Assert.NotEmpty(model.Error);
        model.FileName = "пример.circ";
        model.AcceptCommand.Execute(null);
        Assert.Equal(circuit, accepted);
    }

    [Fact]
    public void SaveAppendsExtensionAndAsksBeforeReplacing()
    {
        using var environment = new Stage4TestEnvironment();
        string? accepted = null;
        var confirm = false;
        var confirmations = new List<string>();
        var model = new FilePickerViewModel("Проекты|*.circ", ".circ", "test", environment.Folder,
            path => accepted = path, path => { confirmations.Add(path); return confirm; });
        model.ShowNewFolderCommand.Execute(null);
        model.NewFolderName = "Экспорт";
        model.CreateFolderCommand.Execute(null);
        var destination = Path.Combine(environment.Folder, "Экспорт", "test.circ");
        Assert.Equal(Path.GetDirectoryName(destination), model.Address);
        File.WriteAllText(destination, "первоначальное содержимое");

        model.AcceptCommand.Execute(null);
        Assert.Equal(destination, Assert.Single(confirmations));
        Assert.Null(accepted);
        confirm = true;
        model.AcceptCommand.Execute(null);
        Assert.Equal(destination, accepted);
        Assert.Equal("первоначальное содержимое", File.ReadAllText(destination));
    }
}
