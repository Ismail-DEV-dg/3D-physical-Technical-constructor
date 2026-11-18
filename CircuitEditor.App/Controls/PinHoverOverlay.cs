using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace CircuitEditor.App.Controls;
// A separate retained visual: hovering never redraws the component scene/cache.
internal sealed class PinHoverOverlay : DrawingVisual
{
    private string? key;
    private Point position;
    private double lastRadius;
    internal string? ActiveKey=>key;
    internal void Update(string? target,Point point=default,double radius=6)
    {
        if(key==target && (target is null || position==point && lastRadius==radius))return;
        key=target;position=point;lastRadius=radius;
        using var dc=RenderOpen();if(target is null)return;
        var glow=new RadialGradientBrush(Color.FromArgb(160,0,229,255),Colors.Transparent);
        dc.DrawEllipse(glow,null,point,radius*2,radius*2);
        var ink=new SolidColorBrush(Color.FromArgb(210,85,225,255));
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(85,0,229,255)),new Pen(ink,1.4),point,radius,radius);
        if(SystemParameters.ClientAreaAnimation)
        {
            var fade=new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(120));
            glow.BeginAnimation(Brush.OpacityProperty,fade);ink.BeginAnimation(Brush.OpacityProperty,fade);
        }
    }
}
