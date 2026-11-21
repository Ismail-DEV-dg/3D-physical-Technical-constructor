using System.Windows.Media.Media3D;

namespace CircuitEditor.App.Controls.ThreeD;

internal sealed record SceneReaction(Guid Node, string Role, GeometryModel3D Model, Transform3D? Transform);
