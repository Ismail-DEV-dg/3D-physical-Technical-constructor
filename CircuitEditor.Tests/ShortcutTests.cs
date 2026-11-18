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
        Assert.Equal(ShortcutCatalog.Bindings.Count,ShortcutCatalog.Bindings.Select(b=>(b.Key,b.Modifiers)).Distinct().Count());
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
    public void ContextAndExactModifiersPreventTypingConflicts(Key key,ModifierKeys mods,bool text,bool canvas,string? expected)
    {Assert.Equal(expected,ShortcutCatalog.Resolve(key,mods,text,canvas)?.Id);}
    [Fact]
    public void GuideSearchFindsActionsAndMouseGestures()
    {
        var vm=new ShortcutGuideViewModel();vm.Search="breadboard";Assert.Contains(vm.Entries,e=>e.Description.Contains("breadboard"));
        vm.Search="Ctrl+Shift+S";Assert.Single(vm.Entries);Assert.Equal("SaveAs",vm.Entries[0].Id);
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
        ShortcutCommands.Execute(vm,"NextTab");Assert.Equal(WorkspaceKind.Chip,vm.SelectedWorkspace.Kind);
        ShortcutCommands.Execute(vm,"PreviousTab");Assert.Equal(WorkspaceKind.Schematic,vm.SelectedWorkspace.Kind);
        var before=e.Project.ComponentCount;e.Project.BeginGesture();ShortcutCommands.Execute(vm,"Duplicate");Assert.Equal(before,e.Project.ComponentCount);
        ShortcutCommands.Execute(vm,"Cancel");Assert.False(e.Project.InGesture);
    }
}
