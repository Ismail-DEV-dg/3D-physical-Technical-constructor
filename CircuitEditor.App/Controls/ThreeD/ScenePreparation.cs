using System.Collections.Immutable;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.App.Controls.ThreeD;

internal sealed class ScenePreparation(ISceneMeshFactory meshes)
{
    public PreparedScene Build(SceneModel scene, CancellationToken token)
    {
        var nodes = ImmutableArray.CreateBuilder<PreparedSceneNode>(scene.Nodes.Length);
        foreach (var node in scene.Nodes)
        {
            token.ThrowIfCancellationRequested();
            var parts = meshes.Build(node, token);
            nodes.Add(new(node, parts, Pose(node.Pose), Outline(node, parts)));
        }
        return new(scene.Bounds, nodes.MoveToImmutable(), Lights());
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
            return parts.Length == 0 ? null : SceneMeshes.Model(parts[0].Model.Geometry, SceneMaterials.Solid(outline));

        const double margin = .48;
        Rect3D bounds = Rect3D.Empty;
        foreach (var p in parts)
            if (p.Role != SceneMeshes.Label) bounds.Union(p.Model.Bounds);
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
        Model3DGroup lights = new();
        lights.Children.Add(new AmbientLight(Color.FromRgb(117, 117, 127)));
        lights.Children.Add(new DirectionalLight(Color.FromRgb(200, 196, 185), new(-.4, .65, -1)));
        lights.Children.Add(new DirectionalLight(Color.FromRgb(86, 105, 122), new(.5, -.5, -.6)));
        lights.Freeze();
        return lights;
    }
}
