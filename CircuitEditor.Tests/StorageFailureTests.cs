using System.IO;
using CircuitEditor.Core.Documents;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

public sealed class StorageFailureTests
{
    [Fact]
    public void JournalRotatesAndExcludesInputAndPaths()
    {
        using var fixture = new Stage4TestEnvironment();
        var logs = Path.Combine(fixture.Folder, "logs");
        var repository = new ErrorLogRepository(logs, maximumBytes: 128);
        var personalDetails = Path.Combine(fixture.Folder, "private-project.circ");
        for (var i = 0; i < 8; i++)
            Assert.True(repository.TryAppend(new IOException(personalDetails)));
        Assert.Equal(2, Directory.GetFiles(logs).Length);
        foreach (var file in Directory.GetFiles(logs))
        {
            var content = File.ReadAllText(file);
            Assert.Contains("System.IO.IOException", content);
            Assert.DoesNotContain("private-project", content);
            Assert.True(new FileInfo(file).Length <= 128);
        }
    }

    [Theory]
    [InlineData(unchecked((int)0x80070070), "недостаточно места")]
    [InlineData(unchecked((int)0x80070020), "занят")]
    public void WindowsIoFailuresUsePlainMessages(int code, string explanation)
    {
        const string raw = "private file path";
        var message = FileFailure.Describe(new IOException(raw, code));
        Assert.Contains(explanation, message);
        Assert.DoesNotContain(raw, message);
    }

    [Fact]
    public void LockedOrBrokenFileLeavesOpenProjectAndOriginalBytes()
    {
        using var fixture = new Stage4TestEnvironment();
        var path = Path.Combine(fixture.Folder, "test.circ");
        var before = fixture.Project.Model;
        Assert.Null(fixture.Repository.Save(path, fixture.Session.Capture()));
        var bytes = File.ReadAllBytes(path);
        using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.False(fixture.Session.Load(path));
            Assert.Contains("занят", Assert.Single(fixture.Errors));
            Assert.NotNull(fixture.Repository.Save(path, new ProjectDocument()));
        }
        Assert.Equal(bytes, File.ReadAllBytes(path));
        File.WriteAllText(path, "{ malformed");
        Assert.False(fixture.Session.Load(path));
        Assert.Contains("повреждён", fixture.Errors.Last());
        Assert.Same(before, fixture.Project.Model);
    }

    [Fact]
    public void InvalidChipAndMissingLibraryFolderAreHandled()
    {
        using var fixture = new Stage4TestEnvironment();
        var folder = Path.Combine(fixture.Folder, "chips");
        var library = new JsonChipRepository(new JsonLogicFamilyRepository(), [], folder);
        Assert.Empty(library.List().Value!);
        var damaged = Path.Combine(folder, "damaged.chip");
        File.WriteAllText(damaged, "{ invalid");
        Assert.Contains("повреждён", library.Load(damaged).Error);
        Assert.Contains("разрешения", FileFailure.Describe(new UnauthorizedAccessException("raw")));
    }

    [Fact]
    public void CrashCopyDoesNotReplaceActiveDocument()
    {
        using var fixture = new Stage4TestEnvironment();
        var original = Path.Combine(fixture.Folder, "active.circ");
        var copy = Path.Combine(fixture.Folder, "copy.circ");
        fixture.Target = original;
        fixture.Session.SaveCommand.Execute(null);
        var bytes = File.ReadAllBytes(original);
        fixture.Project.Add(fixture.Project.Library.First());
        Assert.False(fixture.Session.SaveRecoveryCopy());
        fixture.Target = copy;
        Assert.True(fixture.Session.SaveRecoveryCopy());
        Assert.True(fixture.Project.IsDirty);
        Assert.Equal(bytes, File.ReadAllBytes(original));
        Assert.True(fixture.Repository.Load(copy).Success);
        Assert.True(fixture.Session.RequestClose());
    }
}
