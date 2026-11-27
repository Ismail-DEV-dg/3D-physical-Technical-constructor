using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.App.Controls.ThreeD;

internal sealed class ScenePicking(Visual3D root, IReadOnlyDictionary<Guid, SceneVisualNode> nodes)
{
    private readonly ScenePickIndex<Visual3D> picks = new();

    public void Add(SceneVisualNode node)
    {
        picks.Set(node.Body, node.Node.Id);
        foreach (var child in node.Body.Children) picks.Set(child, node.Node.Id);
    }

    public void Remove(SceneVisualNode node)
    {
        picks.Remove(node.Body);
        foreach (var child in node.Body.Children) picks.Remove(child);
    }

    public SceneNode? Find(Viewport3D viewport, Point at)
    {
        SceneNode? result = null;
        var distance = double.PositiveInfinity;
        VisualTreeHelper.HitTest(viewport, Filter, Record, new PointHitTestParameters(at));
        return result;

        HitTestFilterBehavior Filter(DependencyObject candidate) =>
            candidate is Visual3D visual && visual != root && picks.Find(visual) is null
                ? HitTestFilterBehavior.ContinueSkipSelfAndChildren : HitTestFilterBehavior.Continue;

        HitTestResultBehavior Record(HitTestResult hit)
        {
            if (hit is RayMeshGeometry3DHitTestResult mesh && mesh.DistanceToRayOrigin < distance &&
                picks.Find(mesh.VisualHit) is { } id && nodes.TryGetValue(id, out var node))
            {
                distance = mesh.DistanceToRayOrigin;
                result = node.Node;
            }
            return HitTestResultBehavior.Continue;
        }
    }
}
