using System.Collections.Immutable;
using System.Windows.Media.Media3D;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.App.Controls.ThreeD;

internal sealed record PreparedScene(SceneBounds Bounds, ImmutableArray<PreparedSceneNode> Nodes, Model3DGroup Lights);
