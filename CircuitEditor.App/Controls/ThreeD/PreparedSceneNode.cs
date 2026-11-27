using System.Collections.Immutable;
using System.Windows.Media.Media3D;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.App.Controls.ThreeD;

internal sealed record PreparedSceneNode(SceneNode Node, ImmutableArray<SceneMeshPart> Parts, Transform3D Transform,
    GeometryModel3D? Outline, string GeometryKey, ImmutableArray<SceneMeshPart> Labels);
