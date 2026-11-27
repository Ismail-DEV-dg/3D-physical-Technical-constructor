using System.Windows.Media.Media3D;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.App.Controls.ThreeD;

internal sealed class SceneVisualNode
{
    private readonly Dictionary<string, ModelVisual3D> animated = new(StringComparer.Ordinal);
    private GeometryModel3D? wireModel;

    public SceneVisualNode(PreparedSceneNode source)
    {
        Source = source;
        Body = new() { Transform = source.Transform };
        Labels = new() { Transform = source.Transform };
        Outline = new() { Transform = source.Transform, Content = source.Outline };
        if (SceneShadow.Create(source.Node) is { } shadow)
            Shadow = new ModelVisual3D { Content = shadow, Transform = source.Transform };
        var body = new Model3DGroup();
        foreach (var part in source.Parts)
        {
            if (part.Role is "" or SceneMeshes.Label or SceneMeshes.SurfaceRole)
            {
                body.Children.Add(part.Model);
                continue;
            }
            var visual = new ModelVisual3D { Content = part.Model };
            animated.Add(part.Role, visual);
            Body.Children.Add(visual);
        }
        if (body.Children.Count > 0)
        {
            body.Freeze();
            Body.Content = body;
        }
        if (!source.Labels.IsEmpty)
        {
            var labels = new Model3DGroup();
            foreach (var label in source.Labels) labels.Children.Add(label.Model);
            labels.Freeze();
            Labels.Content = labels;
        }
        if (source.Node.Layer == SceneLayer.Wire) wireModel = source.Parts[0].Model;
    }

    public PreparedSceneNode Source { get; }
    public SceneNode Node => Source.Node;
    public ModelVisual3D Body { get; }
    public ModelVisual3D Labels { get; }
    public ModelVisual3D Outline { get; }
    public ModelVisual3D? Shadow { get; }

    public void React(SceneReaction value, bool selected)
    {
        if (!animated.TryGetValue(value.Role, out var visual)) return;
        if (value.Role == "wire")
        {
            wireModel = value.Model;
            SelectWire(selected);
            return;
        }
        if (!ReferenceEquals(visual.Content, value.Model)) visual.Content = value.Model;
        var transform = value.Transform ?? Transform3D.Identity;
        if (!ReferenceEquals(visual.Transform, transform)) visual.Transform = transform;
    }

    public void SelectWire(bool selected)
    {
        if (!animated.TryGetValue("wire", out var wire)) return;
        var value = selected ? Source.Outline : wireModel;
        if (!ReferenceEquals(wire.Content, value)) wire.Content = value;
    }
}
