using CircuitEditor.App.Interaction;
using CircuitEditor.Core.Layout;

namespace CircuitEditor.App.ViewModels;

internal sealed class MockupSimulationInput(BreadboardViewModel scene)
{
    private Guid? pressed;
    private ComponentViewModel? knob;

    internal bool Handle(CanvasInput input, LayoutPoint point, SimulationViewModel simulation)
    {
        if (input.Action is CanvasAction.Up or CanvasAction.Cancel)
        {
            if (pressed is Guid id) simulation.Press(id, false);
            pressed = null;
            knob = null;
            return false;
        }
        if (input.Action == CanvasAction.Move && knob is { } rotating)
        {
            var previous = simulation.State.Components.GetValueOrDefault(rotating.Model.Id)?.Angle ?? rotating.Model.PotentiometerAngle;
            var local = BreadboardViewModel.KnobPoint(rotating, point.X, point.Y);
            simulation.Turn(rotating.Model.Id, Core.Physical.PotentiometerPosition.FromPointer(local.X, local.Y, previous));
            return true;
        }
        if (input.Pan) return false;
        if (input.Action != CanvasAction.Down) return input.Action is not (CanvasAction.Move or CanvasAction.Wheel);
        var part = scene.Project.Components.LastOrDefault(item => scene.Hit(item, point.X, point.Y));
        scene.Project.Select(part, input.Control);
        if (part?.Model.Definition.Id == "button")
        {
            pressed = part.Model.Id;
            simulation.Press(pressed.Value, true);
        }
        else if (part?.Model.Definition.Id == "switch") simulation.Switch(part.Model.Id);
        else if (part?.Footprint.RotaryControl is { } control)
        {
            var local = BreadboardViewModel.KnobPoint(part, point.X, point.Y);
            if (local.X * local.X + local.Y * local.Y <= control.Radius * control.Radius)
            {
                knob = part;
                Handle(input with { Action = CanvasAction.Move }, point, simulation);
            }
        }
        return true;
    }
}
