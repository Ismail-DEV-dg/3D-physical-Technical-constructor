using CircuitEditor.App.Interaction;
using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Models;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;

namespace CircuitEditor.App.ViewModels;

public sealed partial class BreadboardViewModel
{
    internal PalettePlacementPreview? PalettePreview { get; private set; }
    private (string? Definition, double X, double Y, int ProjectRevision, int? Rotation)? palettePointer;

    private void PreviewPalette(CanvasInput input)
    {
        var key = (input.DefinitionId, CursorX, CursorY, Project.Revision, input.PlacementRotation);
        if (palettePointer == key) return;
        palettePointer = key;
        PalettePreview = FindPaletteDefinition(input.DefinitionId) is { } definition ? PlanPlacement(definition, input.PlacementRotation) : null;
        Revision++;
    }

    private void ClearPalettePreview()
    {
        palettePointer = null;
        if (PalettePreview is null) return;
        PalettePreview = null;
        Revision++;
    }

    private ComponentDefinition? FindPaletteDefinition(string? id) =>
        Project.CanEdit && !Project.InGesture ? Project.PaletteLibrary.FirstOrDefault(item => item.Id == id) : null;

    private PalettePlacementPreview? PlanPlacement(ComponentDefinition definition, int? rotation)
    {
        var footprint = Project.Model.Footprints.FirstOrDefault(item => item.ComponentDefinitionId == definition.Id);
        if (footprint is null && Project.ChipForPlacement(definition.Id) is { } chip)
            footprint = Core.Chips.ChipPackage.Footprint(chip);
        if (footprint is null) return null;
        if (definition.Id == SplitterGeometry.ComponentId)
            footprint = SplitterGeometry.Apply(footprint, SplitterGeometry.DefaultOutputs)!;
        var pose = new ComponentPlacement(CursorX, CursorY, rotation ?? 0);
        if (!footprint.AllowedRotations.Contains(pose.Rotation)) return null;
        if (footprint.Mounting != "breadboard")
        {
            var bounds = ContactGeometry.Bounds(footprint, pose, PhysicalSettings.FreeGridMm);
            pose.MoveTo(CursorX - bounds.X - bounds.Width / 2, CursorY - bounds.Y - bounds.Height / 2);
        }
        if (footprint.Mounting != "breadboard" || Board is not { } board ||
            !BreadboardGeometry.Contains(board, new LayoutPoint(CursorX, CursorY)))
            return new(footprint, pose, null);

        var result = insertion.PreviewNew(Project.Model, definition, footprint, board.Id, new(CursorX, CursorY), rotation);
        return new(footprint, result.Placement ?? pose, result);
    }

    private void DropFromPalette(CanvasInput input)
    {
        ClearPalettePreview();
        if (FindPaletteDefinition(input.DefinitionId) is not { } definition ||
            PlanPlacement(definition, input.PlacementRotation) is not { } plan) return;
        if (plan.Insertion is { Success: false } failure)
        {
            Project.StatusMessage = failure.Error!;
            return;
        }
        SelectTool("Select");
        if (!Project.BeginGesture("Размещение из библиотеки")) return;
        if (Project.Add(definition) is { } added && TryPlace(added, plan.Pose))
        {
            Project.EndGesture();
            return;
        }
        var error = Project.StatusMessage;
        Project.CancelGesture();
        Project.StatusMessage = error;
    }
}
