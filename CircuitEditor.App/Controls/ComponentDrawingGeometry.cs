using System.Windows.Media;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Physical;

namespace CircuitEditor.App.Controls;

internal sealed record ComponentDrawingGeometry(double X, double Y, int Rotation, double Pitch,
    FootprintDefinition Footprint, LayoutRect Bounds, MatrixTransform Transform);
