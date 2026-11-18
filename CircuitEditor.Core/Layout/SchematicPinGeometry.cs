using CircuitEditor.Core.Placement;
namespace CircuitEditor.Core.Layout;
/// <summary>Положение вывода повёрнутого символа без зависимости от рисовальщика.</summary>
public static class SchematicPinGeometry
{
    /// <summary>Возвращает мировые координаты доступного вывода.</summary>
    public static LayoutPoint? Position(ComponentLayout layout,ComponentPlacement placement,string pinId)
    {
        var pin=layout.Pins.FirstOrDefault(p=>p.Pin.Id==pinId);if(pin is null)return null;
        var angle=placement.Rotation*Math.PI/180;var dx=pin.Position.X-layout.Width/2;var dy=pin.Position.Y-layout.Height/2;
        return new(placement.X+layout.Width/2+dx*Math.Cos(angle)-dy*Math.Sin(angle),
            placement.Y+layout.Height/2+dx*Math.Sin(angle)+dy*Math.Cos(angle));
    }
}
