using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Scene;

namespace CircuitEditor.App.Controls.ThreeD;

internal static class SplitterMeshes
{
    private const double RimWidth = 2.35;
    private const string Case = "#215951";

    internal static void Draw(SceneMeshes m, SceneNode node)
    {
        var b = node.Body;
        var top = node.Profile.Height;
        var floor = top - SplitterGeometry.SocketDepth;
        m.BeveledBox(b, 0, .87, "#132B2C", corner: .83, bevel: .19);
        m.BeveledBox(b, .76, floor - .76 - SceneMeshes.RecessBackingClearance, Case, corner: .70, bevel: .17);
        m.SideWalls(b, floor - .15, top, Case);
        var left = b.X;
        for (var i = 0; i < node.Contacts.Length; i++)
        {
            var pin = node.Contacts[i].Position;
            var right = i + 1 == node.Contacts.Length ? b.Right : (pin.X + node.Contacts[i + 1].Position.X) / 2;
            var opening = new LayoutRect(pin.X - RimWidth / 2, pin.Y - RimWidth / 2, RimWidth, RimWidth);
            m.Recess(new(left, b.Y, right - left, b.Height), opening, top, SplitterGeometry.SocketDepth,
                node.Profile.Material, "#7EAB97", "#202B2A", chamfer: .23);
            m.Ring(pin.X, pin.Y, floor + .02, .17, 1.87, 1.03, i == 0 ? "#E5BF75" : SceneMeshes.Gold);
            left = right;
        }
        if (node.Footprint is { } footprint)
            m.Face(b, top, new(b, dc => SplitterDrawing.Markings(dc, footprint.Contacts)));
    }
}
