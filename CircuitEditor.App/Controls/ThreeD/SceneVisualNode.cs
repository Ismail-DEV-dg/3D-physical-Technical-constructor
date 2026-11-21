using System.Windows.Media.Media3D;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.App.Controls.ThreeD;

internal sealed class SceneVisualNode
{
    public SceneVisualNode(PreparedSceneNode source)
    {
        Source = source;
        Body = new() { Transform = source.Transform };
        Labels = new() { Transform = source.Transform };
        Outline = new() { Transform = source.Transform, Content = source.Outline };
        foreach (var part in source.Parts)
            (part.Role == SceneMeshes.Label ? Labels : Body).Children.Add(new ModelVisual3D { Content = part.Model });
    }

    public PreparedSceneNode Source { get; }
    public SceneNode Node => Source.Node;
    public ModelVisual3D Body { get; }
    public ModelVisual3D Labels { get; }
    public ModelVisual3D Outline { get; }
}
