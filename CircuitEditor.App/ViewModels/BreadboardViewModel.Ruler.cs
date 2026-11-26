using CircuitEditor.App.Interaction;
using CircuitEditor.Core.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class BreadboardViewModel
{
    [ObservableProperty] private bool isRulerActive;
    internal LayoutPoint? RulerStart { get; private set; }
    internal LayoutPoint? RulerEnd { get; private set; }
    private bool rulerComplete;

    [RelayCommand]
    private void ToggleRuler()
    {
        if (Project.InGesture) return;
        var activate = !IsRulerActive;
        SelectTool("Select");
        IsRulerActive = activate;
        Project.StatusMessage = activate ? "Линейка: укажите две точки. Esc — выйти." : "Линейка выключена.";
    }

    private bool RouteRuler(CanvasInput input)
    {
        if (!IsRulerActive || input.Pan || input.Action is not (CanvasAction.Down or CanvasAction.Move)) return false;
        var point = new LayoutPoint(CursorX, CursorY);
        if (input.Action == CanvasAction.Down)
        {
            if (RulerStart is null || rulerComplete)
            {
                RulerStart = point;
                RulerEnd = point;
                rulerComplete = false;
            }
            else
            {
                RulerEnd = point;
                rulerComplete = true;
                var start = RulerStart.Value;
                var dx = start.X - point.X;
                var dy = start.Y - point.Y;
                Project.StatusMessage = $"Расстояние: {Math.Sqrt(dx * dx + dy * dy):0.##} мм";
            }
        }
        else if (RulerStart is not null && !rulerComplete)
            RulerEnd = point;
        else
            return false;

        Revision++;
        return true;
    }

    private void ClearRuler()
    {
        if (!IsRulerActive) return;
        IsRulerActive = false;
        RulerStart = null;
        RulerEnd = null;
        rulerComplete = false;
        Revision++;
    }
}
