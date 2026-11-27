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
        var direction = view switch
        {
            SceneCameraView.Top => new Vector3D(0, 0, -1),
            SceneCameraView.Front => new Vector3D(0, 1, 0),
            SceneCameraView.Isometric => new Vector3D(-1, 1, -1),
            SceneCameraView.Fit => camera.LookDirection,
            _ => new Vector3D(.34, .72, -1)
        };
        if (direction.LengthSquared == 0) direction = new(0, 0, -1);
        direction.Normalize();
        var up = view == SceneCameraView.Fit ? camera.UpDirection :
            view == SceneCameraView.Top || view == SceneCameraView.Initial ? new Vector3D(0, 1, 0) : new(0, 0, 1);
        var fieldOfView = camera is PerspectiveCamera perspective ? perspective.FieldOfView : 45;
        var horizontal = fieldOfView * Math.PI / 360;
        var vertical = Math.Atan(Math.Tan(horizontal) / Math.Max(.1, aspect));
        var right = Vector3D.CrossProduct(direction, up);
        right.Normalize();
        up = Vector3D.CrossProduct(right, direction);
        var tanX = Math.Tan(horizontal);
        var tanY = Math.Tan(vertical);
        var distance = MinimumRadius;
        var half = (maximum - minimum) / 2;
        foreach (var x in new[] { -half.X, half.X })
            foreach (var y in new[] { -half.Y, half.Y })
                foreach (var z in new[] { -half.Z, half.Z })
                {
                    var corner = new Vector3D(x, y, z);
                    var depth = Vector3D.DotProduct(corner, direction);
                    var horizontalFit = Math.Abs(Vector3D.DotProduct(corner, right)) / tanX;
                    var verticalFit = Math.Abs(Vector3D.DotProduct(corner, up)) / tanY;
                    distance = Math.Max(distance, Math.Max(horizontalFit, verticalFit) * FitMargin - depth);
                }
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
