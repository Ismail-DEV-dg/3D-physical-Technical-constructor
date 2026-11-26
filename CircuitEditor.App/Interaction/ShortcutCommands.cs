using CircuitEditor.App.ViewModels;
using CommunityToolkit.Mvvm.Input;
namespace CircuitEditor.App.Interaction;
public static class ShortcutCommands
{
    public static void Execute(MainViewModel vm,string id)
    {
        var canvas=vm.SelectedWorkspace.Kind is WorkspaceKind.Mockup or WorkspaceKind.Schematic;
        void Input(CanvasAction action)=> (vm.SelectedWorkspace.Kind==WorkspaceKind.Mockup ? vm.Mockup.InputCommand:vm.Schematic.InputCommand).Execute(new CanvasInput(action));
        if(vm.Project.InGesture && id is not ("Cancel" or "Select" or "Help"))
        {vm.StatusMessage="Завершите жест или нажмите Esc перед другой командой.";return;}
        IRelayCommand? command=id switch
        {
            "Help"=>vm.ShortcutHelpCommand,"New"=>vm.Session?.NewCommand,"Open"=>vm.Session?.OpenCommand,
            "Documentation"=>vm.DocumentationCommand,
            "Save"=>vm.Session?.SaveCommand,"SaveAs"=>vm.Session?.SaveAsCommand,
            "Undo"=>vm.UndoActiveCommand,"Redo"=>vm.RedoActiveCommand,"Copy"=>vm.CopyActiveCommand,"Paste"=>vm.PasteActiveCommand,
            "Check"=>vm.Analysis?.CheckCommand,"Calculate"=>vm.Analysis?.CalculateCommand,"Calculator"=>vm.CalculatorCommand,
            "RunSketch"=>vm.Simulation?.ToggleSketchCommand,
            "SimulationPanel"=>vm.Simulation?.ShowConfigurationCommand,
            "CodeFind"=>vm.Code.FindCommand,"CodeReplace"=>vm.Code.ReplaceCommand,"CodeLine"=>vm.Code.GoToCommand,
            "Theme"=>vm.ToggleThemeCommand,"Png"=>vm.Exports?.PngCommand,"Svg"=>vm.Exports?.SvgCommand,"Bom"=>vm.Exports?.BomCommand,_=>null
        };
        if(command is not null){if(command.CanExecute(null))command.Execute(null);return;}
        if (id.StartsWith("Code", StringComparison.Ordinal))
        {
            var action = id[4..];
            if (action is "FindNext" or "FindPrevious" && string.IsNullOrEmpty(vm.Code.SearchText))
                vm.Code.FindCommand.Execute(null);
            else
                vm.Code.EditorActionCommand.Execute(action);
            return;
        }
        if(id.StartsWith("Tab",StringComparison.Ordinal)&&int.TryParse(id.AsSpan(3),out var index)&&(uint)index<(uint)vm.Workspaces.Count)vm.SelectedWorkspace=vm.Workspaces[index];
        else if(id is "NextTab" or "PreviousTab")
        {var indexOf=vm.Workspaces.ToList().IndexOf(vm.SelectedWorkspace);vm.SelectedWorkspace=vm.Workspaces[(indexOf+(id=="NextTab"?1:vm.Workspaces.Count-1))%vm.Workspaces.Count];}
        else if(canvas)
        {
            switch(id)
            {
                case "Cancel":case "Select":Input(CanvasAction.Cancel);break;
                case "Rotate":Input(CanvasAction.Rotate);break;
                case "Delete":Input(CanvasAction.Delete);break;
                case "SelectAll":Input(CanvasAction.SelectAll);break;
                case "Wire":if(vm.IsMockup)vm.Mockup.SelectToolCommand.Execute(vm.Project.Wiring.IsWiring ? "Select" : "Wire");break;
                case "Note":if(vm.IsMockup)vm.Mockup.BeginNoteCommand.Execute(null);break;
                case "Measure":if(vm.IsMockup)vm.Mockup.ToggleRulerCommand.Execute(null);break;
                case "WireLabels":if(vm.IsMockup)vm.Mockup.ToggleWireLabelsCommand.Execute(null);break;
                case "Grid":if(vm.IsMockup)vm.Mockup.ShowGrid=!vm.Mockup.ShowGrid;break;
                case "InspectNet":if(vm.IsMockup)vm.Mockup.IsNetInspectionActive=!vm.Mockup.IsNetInspectionActive;break;
                case "FocusSelection":if(vm.IsMockup)vm.Mockup.FocusSelectionCommand.Execute(null);break;
                case "Duplicate":if(vm.Project.SelectedCount>0){vm.Project.CopyCommand.Execute(null);vm.Project.PasteCommand.Execute(null);}break;
                case "Fit":WorkspaceViewport.Fit(vm);break;
                case "Reset":(vm.SelectedWorkspace.Kind==WorkspaceKind.Mockup?vm.Mockup.ResetViewCommand:vm.Schematic.ResetViewCommand).Execute(null);break;
                case "ZoomIn":case "ZoomOut":
                    var mock=vm.SelectedWorkspace.Kind==WorkspaceKind.Mockup;
                    (mock?vm.Mockup.InputCommand:vm.Schematic.InputCommand).Execute(new CanvasInput(CanvasAction.Wheel,
                        (mock?vm.Mockup.ViewportWidth:vm.Schematic.ViewportWidth)/2,(mock?vm.Mockup.ViewportHeight:vm.Schematic.ViewportHeight)/2,Delta:id=="ZoomIn"?120:-120));break;
            }
        }
    }
}
