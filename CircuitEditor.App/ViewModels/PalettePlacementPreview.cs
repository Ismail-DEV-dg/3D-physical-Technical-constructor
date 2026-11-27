using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;
using CircuitEditor.Core.Services;

namespace CircuitEditor.App.ViewModels;

internal sealed record PalettePlacementPreview(FootprintDefinition Footprint, ComponentPlacement Pose, InsertionResult? Insertion);
