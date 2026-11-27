using System.Windows.Media.Media3D;

namespace CircuitEditor.App.Controls.ThreeD;

internal sealed record SceneMeshPart(string Role, string MaterialKey, GeometryModel3D Model, Point3D Pivot = default);
