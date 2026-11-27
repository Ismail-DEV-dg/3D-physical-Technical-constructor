using CircuitEditor.App.Interaction;
using CircuitEditor.Core.Breadboards;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Physical;
namespace CircuitEditor.App.ViewModels;
public sealed class BoardInteractionViewModel(BreadboardViewModel scene)
{
    private bool dragging;
    private double grabX,grabY;
    internal bool IsDragging => dragging;
    public bool Selected {get;private set;}
    public bool Handle(CanvasInput input,LayoutPoint point)
    {
        var project=scene.Project;var board=scene.Board;if(board is null)return false;
        if(input.Action==CanvasAction.Cancel){if(dragging)project.CancelGesture();dragging=false;return false;}
        if(input.Action==CanvasAction.Rotate && Selected)
        {
            if (dragging)
            {
                (grabX, grabY) = (-grabY, grabX);
                project.Model.TransformBoard(board.Id, point.X - grabX, point.Y - grabY, (board.Rotation + 90) % 360);
                project.Wiring.InvalidatePaths();
                project.Refresh();
                return true;
            }
            if (!project.BeginGesture("Поворот breadboard")) return false;
            var center=new LayoutPoint(board.Definition.Width/2,board.Definition.Height/2);
            var world=BreadboardGeometry.World(board,center);var next=board with {X=0,Y=0,Rotation=(board.Rotation+90)%360};
            var offset=BreadboardGeometry.World(next,center);
            project.Model.TransformBoard(board.Id,world.X-offset.X,world.Y-offset.Y,next.Rotation);
            project.EndGesture();project.Refresh();return true;
        }
        if(dragging)
        {
            if(input.Action==CanvasAction.Move)
            {project.Model.TransformBoard(board.Id,PhysicalSettings.SnapFree(point.X-grabX),PhysicalSettings.SnapFree(point.Y-grabY),board.Rotation);project.Wiring.InvalidatePaths();project.Refresh();}
            if(input.Action==CanvasAction.Up){dragging=false;project.EndGesture();project.Refresh();}
            return true;
        }
        if(input.Action!=CanvasAction.Down||input.Pan)return false;
        if(project.Wiring.IsWiring||project.Components.Any(c=>scene.Hit(c,point.X,point.Y))){Selected=false;return false;}
        var local=BreadboardGeometry.Local(board,point);
        const double BorderGripMm=1.5;
        var border=local.X<BorderGripMm||local.Y<BorderGripMm||local.X>board.Definition.Width-BorderGripMm||local.Y>board.Definition.Height-BorderGripMm;
        if(!border){Selected=false;return false;}
        Selected=BreadboardGeometry.Contains(board,point);
        if(!Selected)return false;
        project.Select(null);project.BeginGesture("Перемещение breadboard");grabX=point.X-board.X;grabY=point.Y-board.Y;dragging=true;
        project.StatusMessage="Выбрана breadboard: перетащите корпус; R — поворот со вставленными деталями.";return true;
    }
}
