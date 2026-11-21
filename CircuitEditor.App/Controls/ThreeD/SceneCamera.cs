using System.Windows.Media.Media3D;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Scene;
using HelixToolkit.Wpf;

namespace CircuitEditor.App.Controls.ThreeD;

internal static class SceneCamera
{
    private const double AnimationMilliseconds = 260;
    private const double FitMargin = 1.18;
    private const double MinimumRadius = 6;

    public static void Apply(ProjectionCamera camera, SceneBounds bounds, SceneCameraView view, double aspect, bool animated)
    {
        var minimum = SceneMeshes.Point(bounds.Min);
        var maximum = SceneMeshes.Point(bounds.Max);
        var center = minimum + (maximum - minimum) * .5;
        var radius = Math.Max(MinimumRadius, (maximum - minimum).Length * .5);
        var direction = view switch
        {
            SceneCameraView.Top => new Vector3D(0, 0, -1),
            SceneCameraView.Front => new Vector3D(0, 1, 0),
            SceneCameraView.Isometric => new Vector3D(-1, 1, -1),
            SceneCameraView.Fit => camera.LookDirection,
            _ => new Vector3D(.08, .32, -1)
        };
        if (direction.LengthSquared == 0) direction = new(0, 0, -1);
        direction.Normalize();
        var up = view == SceneCameraView.Fit ? camera.UpDirection :
            view == SceneCameraView.Top || view == SceneCameraView.Initial ? new Vector3D(0, 1, 0) : new(0, 0, 1);
        var fieldOfView = camera is PerspectiveCamera perspective ? perspective.FieldOfView : 45;
        var horizontal = fieldOfView * Math.PI / 360;
        var vertical = Math.Atan(Math.Tan(horizontal) / Math.Max(.1, aspect));
        var distance = radius * FitMargin / Math.Sin(Math.Min(horizontal, vertical));
        var look = direction * distance;
        if (animated) camera.AnimateTo(center - look, look, up, AnimationMilliseconds);
        else
        {
            camera.Position = center - look;
            camera.LookDirection = look;
            camera.UpDirection = up;
        }
    }
}
