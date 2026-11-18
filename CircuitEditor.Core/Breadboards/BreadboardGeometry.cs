using CircuitEditor.Core.Layout;
namespace CircuitEditor.Core.Breadboards;
/// <summary>Единственное преобразование локальных отверстий повёрнутой breadboard.</summary>
public static class BreadboardGeometry
{
    /// <summary>Локальная точка в координаты стола.</summary>
    public static LayoutPoint World(BreadboardInstance b,LayoutPoint p)
    {var a=b.Rotation*Math.PI/180;return new(b.X+p.X*Math.Cos(a)-p.Y*Math.Sin(a),b.Y+p.X*Math.Sin(a)+p.Y*Math.Cos(a));}
    /// <summary>Координаты стола в локальные.</summary>
    public static LayoutPoint Local(BreadboardInstance b,LayoutPoint p)
    {var a=-b.Rotation*Math.PI/180;var x=p.X-b.X;var y=p.Y-b.Y;return new(x*Math.Cos(a)-y*Math.Sin(a),x*Math.Sin(a)+y*Math.Cos(a));}
    /// <summary>Попадание в корпус независимо от поворота.</summary>
    public static bool Contains(BreadboardInstance b,LayoutPoint p)
    {var q=Local(b,p);return q.X>=0&&q.Y>=0&&q.X<=b.Definition.Width&&q.Y<=b.Definition.Height;}
}
