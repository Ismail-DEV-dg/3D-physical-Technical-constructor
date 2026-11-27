using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.App.Controls.ThreeD;

internal sealed class ScenePreparation(ISceneMeshFactory meshes)
{
    private const int CacheLimit = 512;
    private const int MaximumLabels = 80;
    private readonly ConcurrentDictionary<string, (ImmutableArray<SceneMeshPart> Parts, GeometryModel3D? Outline)> cache = new();
    private readonly Lazy<Model3DGroup> lights = new(Lights);

    public PreparedScene Build(SceneModel scene, CancellationToken token, PreparedScene? previous = null)
    {
        var nodes = ImmutableArray.CreateBuilder<PreparedSceneNode>(scene.Nodes.Length);
        var known = previous?.Nodes.ToDictionary(n => n.Node.Id);
        var labels = 0;
        foreach (var node in scene.Nodes)
        {
            token.ThrowIfCancellationRequested();
            var key = SceneGeometryKey.Of(node);
            var showLabel = node.Label.Length > 0 && labels++ < MaximumLabels;
            PreparedSceneNode? old = known?.GetValueOrDefault(node.Id);
            if (old?.GeometryKey == key && old.Node.Label == node.Label && old.Node.Name == node.Name &&
                !old.Labels.IsEmpty == showLabel && old.Node.Pose == node.Pose && old.Node.Angle == node.Angle &&
                old.Node.SwitchPosition == node.SwitchPosition && old.Node.Value == node.Value)
            {
                nodes.Add(old);
                continue;
            }
            var current = old?.GeometryKey == key ? (old.Parts, old.Outline) :
                cache.TryGetValue(key, out var hit) ? hit : PrepareGeometry(node, key, token);
            var label = old is not null && old.GeometryKey == key && old.Node.Label == node.Label && old.Node.Name == node.Name
                && !old.Labels.IsEmpty == showLabel ? old.Labels : showLabel ? Label(node) : [];
            nodes.Add(new(node, current.Parts, old?.Node.Pose == node.Pose ? old.Transform : Pose(node.Pose),
                current.Outline, key, label));
        }
        return new(scene.Bounds, nodes.MoveToImmutable(), lights.Value);
    }

    private (ImmutableArray<SceneMeshPart> Parts, GeometryModel3D? Outline) PrepareGeometry(
        SceneNode node, string key, CancellationToken token)
    {
        var parts = meshes.Build(node, token);
        var outline = Outline(node, parts);
        var value = (parts, outline);
        if (cache.Count < CacheLimit) cache.TryAdd(key, value);
        return value;
    }

    private static ImmutableArray<SceneMeshPart> Label(SceneNode node)
    {
        var b = node.Body;
        SceneMeshes m = new();
        m.Text(new(b.X, b.Y - 3.5, Math.Max(8, b.Width), 3), node.Profile.Clearance + node.Profile.Height + node.Profile.DetailHeight,
            node.Label + (node.Family == "unknown" ? " " + node.Name : ""), "#66C8DF");
        return m.Finish();
    }

    private static Transform3D Pose(ScenePose pose)
    {
        var position = SceneMeshes.Point(pose.Origin);
        Transform3DGroup result = new();
        result.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new(0, 0, 1), -pose.Rotation)));
        result.Children.Add(new TranslateTransform3D(position.X, position.Y, position.Z));
        result.Freeze();
        return result;
    }

    private static GeometryModel3D? Outline(SceneNode node, ImmutableArray<SceneMeshPart> parts)
    {
        const string outline = "#28DCF3";
        if (node.Layer == SceneLayer.Wire)
            return parts.Length == 0 ? null : SceneMeshes.Model(parts[0].Model.Geometry, SceneMaterials.Glow(outline, outline, 6));

        const double margin = .48;
        Rect3D bounds = Rect3D.Empty;
        foreach (var p in parts) bounds.Union(p.Model.Bounds);
        if (bounds.IsEmpty) return null;
        var z = bounds.Z + bounds.SizeZ + margin;
        var x = bounds.X - margin;
        var y = -bounds.Y - bounds.SizeY - margin;
        var r = bounds.X + bounds.SizeX + margin;
        var b = -bounds.Y + margin;
        SceneMeshes mesh = new();
        mesh.Tube([new(x, y, z), new(r, y, z), new(r, b, z), new(x, b, z), new(x, y, z)], .38, outline);
        return mesh.Finish()[0].Model;
    }

    private static Model3DGroup Lights()
    {
        Model3DGroup result = new();
        result.Children.Add(new AmbientLight(Color.FromRgb(104, 104, 104)));
        result.Children.Add(new DirectionalLight(Color.FromRgb(176, 173, 168), new(-.5, .7, -1)));
        result.Children.Add(new DirectionalLight(Color.FromRgb(70, 70, 70), new(.7, -.48, -.65)));
        result.Freeze();
        return result;
    }
}
