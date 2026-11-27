using System.Windows.Input;
using CircuitEditor.App.Interaction;
using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Services;
namespace CircuitEditor.Tests;
public sealed class ShortcutTests
{
    [Fact]
    public void BindingsAreUniqueAndAllHaveHelpText()
    {
        Assert.True(ShortcutCatalog.Bindings.Count>=35);
        Assert.Equal(ShortcutCatalog.Bindings.Count,ShortcutCatalog.Bindings.Select(b=>(b.Key,b.Modifiers,b.CodeOnly,b.CanvasOnly)).Distinct().Count());
        Assert.All(ShortcutCatalog.Bindings,b=>{Assert.NotEmpty(b.Keys);Assert.NotEmpty(b.Description);Assert.Contains(b.Keys,ShortcutCatalog.Hint(b.Id));});
    }
    [Theory]
    [InlineData(Key.W,ModifierKeys.None,true,false,null)]
    [InlineData(Key.W,ModifierKeys.None,false,true,"Wire")]
    [InlineData(Key.W,ModifierKeys.Control,false,true,null)]
    [InlineData(Key.C,ModifierKeys.Control,true,false,null)]
    [InlineData(Key.Z,ModifierKeys.Control,true,false,null)]
    [InlineData(Key.S,ModifierKeys.Control,true,false,"Save")]
    [InlineData(Key.F1,ModifierKeys.None,true,false,"Help")]
    [InlineData(Key.F,ModifierKeys.Control,true,false,"Search")]
    [InlineData(Key.R,ModifierKeys.None,false,false,null)]
    [InlineData(Key.B,ModifierKeys.Control|ModifierKeys.Shift,true,false,"RunSketch")]
    [InlineData(Key.Y,ModifierKeys.Control,false,true,"Redo")]
    public void ContextAndExactModifiersPreventTypingConflicts(Key key,ModifierKeys mods,bool text,bool canvas,string? expected)
    {Assert.Equal(expected,ShortcutCatalog.Resolve(key,mods,text,canvas)?.Id);}
    [Fact]
    public void GuideSearchFindsActionsAndMouseGestures()
    {
        var vm=new ShortcutGuideViewModel();vm.Search="breadboard";Assert.Contains(vm.Entries,e=>e.Description.Contains("breadboard"));
        vm.Search="Ctrl+Shift+S";Assert.Single(vm.Entries);Assert.Equal("SaveAs",vm.Entries[0].Id);
    }
    [Fact]
    public void ShortcutChangesPersistAndRejectCollisionsInTheSameContext()
    {
        var repository = new TestShortcutSettingsRepository();
        var settings = new ShortcutSettings(repository);
        ShortcutBinding Duplicate(ShortcutSettings source) => source.Bindings.Single(binding => binding.Id == "CodeDuplicateLine");
        Assert.Contains("уже назначено", settings.Assign(Duplicate(settings), Key.G, ModifierKeys.Control));
        Assert.Null(settings.Assign(Duplicate(settings), Key.D, ModifierKeys.Control | ModifierKeys.Alt));
        Assert.Contains("Ctrl+Alt+D", settings["CodeDuplicateLine"]);
        var reopened = new ShortcutSettings(repository);
        Assert.Equal(ModifierKeys.Control | ModifierKeys.Alt, Duplicate(reopened).Modifiers);
        repository.Error = "Не удалось сохранить";
        Assert.Equal(repository.Error, reopened.Restore(Duplicate(reopened)));
        Assert.Equal(ModifierKeys.Control | ModifierKeys.Alt, Duplicate(reopened).Modifiers);
        repository.Error = null;
        Assert.Null(reopened.RestoreAll());
        Assert.Equal(ModifierKeys.Control, Duplicate(new ShortcutSettings(repository)).Modifiers);
    }

    [Fact]
    public void ShortcutGuideAssignsTheClickedKeycapEvenWhenAnotherRowIsSelected()
    {
        var repository = new TestShortcutSettingsRepository();
        var settings = new ShortcutSettings(repository);
        var guide = new ShortcutGuideViewModel(settings);
        var focus = settings.Bindings.Single(binding => binding.Id == "FocusSelection");
        var reset = settings.Bindings.Single(binding => binding.Id == "Reset");
        guide.Selected = reset;
        Assert.False(guide.Assign(focus, reset.Key, reset.Modifiers));
        Assert.Contains("уже назначено", guide.Feedback);
        Assert.True(guide.Assign(focus, Key.F10, ModifierKeys.Control));
        Assert.Equal(Key.F10, guide.Selected?.Key);
        Assert.Equal(focus.Id, guide.Selected?.Id);
        Assert.Equal(Key.F10, new ShortcutSettings(repository).Bindings.Single(binding => binding.Id == focus.Id).Key);
        Assert.Contains(reset, settings.Bindings);
    }

    [Fact]
    public void IdenticalKeysForCanvasAndCodeHaveSeparateMeaning()
    {
        var bindings = new ShortcutSettings().Bindings;
        var sameGesture = bindings.Where(binding => binding.Key == Key.D && binding.Modifiers == ModifierKeys.Control).ToArray();
        Assert.Equal("Duplicate", Assert.Single(sameGesture, b => b.MatchesContext(false, true, false, false)).Id);
        Assert.Equal("CodeDuplicateLine", Assert.Single(sameGesture, b => b.MatchesContext(true, false, true, true)).Id);
        Assert.DoesNotContain(sameGesture, b => b.MatchesContext(true, false, true, false));
    }

    [Fact]
    public void SavedShortcutsCanReuseAKeyVacatedByALaterAction()
    {
        var repository = new TestShortcutSettingsRepository();
        var settings = new ShortcutSettings(repository);
        ShortcutBinding Get(IShortcutSettings source, string id) => source.Bindings.Single(binding => binding.Id == id);
        Assert.Null(settings.Assign(Get(settings, "Save"), Key.F10, ModifierKeys.Control));
        Assert.Null(settings.Assign(Get(settings, "New"), Key.S, ModifierKeys.Control));
        var reopened = new ShortcutSettings(repository);
        Assert.Equal(Key.S, Get(reopened, "New").Key);
        Assert.Equal(Key.F10, Get(reopened, "Save").Key);
    }
    [Fact]
    public void CommandsToggleModeDuplicateFitAndPreserveGesture()
    {
        using var e=new Stage4TestEnvironment();using var vm=new MainViewModel(e.Navigation,new ThemeService(new TestThemeResources()),new TestWindowService());
        ShortcutCommands.Execute(vm,"Wire");Assert.True(e.Project.Wiring.IsWiring);
        ShortcutCommands.Execute(vm,"Select");Assert.False(e.Project.Wiring.IsWiring);
        e.Project.Add(e.Project.Library.Single(d=>d.Id=="resistor"));var count=e.Project.History.Count;
        ShortcutCommands.Execute(vm,"Duplicate");Assert.Equal(2,e.Project.ComponentCount);Assert.Equal(count+1,e.Project.History.Count);
        ShortcutCommands.Execute(vm,"Fit");Assert.InRange(e.Mockup.Zoom,.25,3);
        ShortcutCommands.Execute(vm,"Tab1");Assert.Equal(WorkspaceKind.Schematic,vm.SelectedWorkspace.Kind);
        ShortcutCommands.Execute(vm,"NextTab");Assert.Equal(WorkspaceKind.Code,vm.SelectedWorkspace.Kind);
        ShortcutCommands.Execute(vm,"PreviousTab");Assert.Equal(WorkspaceKind.Schematic,vm.SelectedWorkspace.Kind);
        var before=e.Project.ComponentCount;e.Project.BeginGesture();ShortcutCommands.Execute(vm,"Duplicate");Assert.Equal(before,e.Project.ComponentCount);
        ShortcutCommands.Execute(vm,"Cancel");Assert.False(e.Project.InGesture);
    }
}
