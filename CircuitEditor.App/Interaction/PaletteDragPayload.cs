using CircuitEditor.Core.Placement;

namespace CircuitEditor.App.Interaction;

internal sealed class PaletteDragPayload(string definitionId)
{
    public string DefinitionId { get; } = definitionId;
    public int? Rotation { get; private set; }
    public Func<int?>? CurrentRotation { get; set; }
    public Action<bool>? RefreshTarget { get; set; }

    public void Rotate()
    {
        Rotation = ((Rotation ?? CurrentRotation?.Invoke() ?? 0) + ComponentPlacement.RotationStep) %
            ComponentPlacement.FullRotation;
        RefreshTarget?.Invoke(false);
    }
}
