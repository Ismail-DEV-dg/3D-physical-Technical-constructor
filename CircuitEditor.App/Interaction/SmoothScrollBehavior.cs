using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CircuitEditor.App.Controls;
namespace CircuitEditor.App.Interaction;
// Presentation-only wheel adapter. Offsets are pixels, never grouped item indexes.
public static class SmoothScrollBehavior
{
    internal const double PixelsPerNotch=32;
    private const double WheelNotch=120;
    private const double MaximumNotchesPerEvent=3;
    private static readonly TimeSpan Duration=TimeSpan.FromMilliseconds(140);
    private static readonly ConditionalWeakTable<ScrollViewer,ScrollMotionState> States=new();
    public static readonly DependencyProperty EnabledProperty=DependencyProperty.RegisterAttached("Enabled",typeof(bool),typeof(SmoothScrollBehavior),new PropertyMetadata(false,EnabledChanged));
    private static readonly DependencyProperty VerticalOffsetProperty=DependencyProperty.RegisterAttached("VerticalOffset",typeof(double),typeof(SmoothScrollBehavior),new PropertyMetadata(0d,(o,e)=>((ScrollViewer)o).ScrollToVerticalOffset((double)e.NewValue)));
    private static readonly DependencyProperty HorizontalOffsetProperty=DependencyProperty.RegisterAttached("HorizontalOffset",typeof(double),typeof(SmoothScrollBehavior),new PropertyMetadata(0d,(o,e)=>((ScrollViewer)o).ScrollToHorizontalOffset((double)e.NewValue)));
    public static void SetEnabled(DependencyObject o,bool value)=>o.SetValue(EnabledProperty,value);
    public static bool GetEnabled(DependencyObject o)=>(bool)o.GetValue(EnabledProperty);
    private static void EnabledChanged(DependencyObject o,DependencyPropertyChangedEventArgs e)
    {
        if(o is not ScrollViewer viewer)return;
        if((bool)e.NewValue)
        {
            viewer.CanContentScroll=false;
            viewer.PreviewMouseWheel+=Wheel;viewer.PreviewMouseDown+=PointerDown;viewer.PreviewKeyDown+=KeyDown;
            viewer.RequestBringIntoView+=BringIntoView;viewer.ScrollChanged+=ScrollChanged;viewer.Unloaded+=Unloaded;
        }
        else
        {
            Stop(viewer);viewer.PreviewMouseWheel-=Wheel;viewer.PreviewMouseDown-=PointerDown;viewer.PreviewKeyDown-=KeyDown;
            viewer.RequestBringIntoView-=BringIntoView;viewer.ScrollChanged-=ScrollChanged;viewer.Unloaded-=Unloaded;
        }
    }
    private static DependencyObject? Parent(DependencyObject o)=>o is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(o)
        : o is FrameworkContentElement f ? f.Parent : LogicalTreeHelper.GetParent(o);
    private static void Wheel(object sender,MouseWheelEventArgs e)
    {
        if(e.Handled || e.Delta==0)return;
        var horizontal=Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        ScrollViewer? target=null;
        for(var node=e.OriginalSource as DependencyObject;node is not null;node=Parent(node))
        {
            if(node is BreadboardSurface or SchematicSurface)return;
            if(node is ScrollViewer candidate && GetEnabled(candidate) && CanMove(candidate,e.Delta,horizontal))
            {target=candidate;break;}
        }
        if(target is null || !ReferenceEquals(target,sender))return;
        Scroll(target,e.Delta,horizontal);e.Handled=true;
    }
    private static bool CanMove(ScrollViewer v,int delta,bool horizontal)
    {
        var state=States.GetOrCreateValue(v);
        var offset=horizontal ? state.HorizontalAnimating?state.HorizontalTarget:v.HorizontalOffset : state.VerticalAnimating?state.VerticalTarget:v.VerticalOffset;
        var maximum=horizontal?v.ScrollableWidth:v.ScrollableHeight;
        return maximum>0 && (delta>0?offset>0:offset<maximum);
    }
    internal static void Scroll(ScrollViewer viewer,int delta,bool horizontal=false,bool animate=true)
    {
        var state=States.GetOrCreateValue(viewer);
        var current=horizontal?viewer.HorizontalOffset:viewer.VerticalOffset;
        var maximum=horizontal?viewer.ScrollableWidth:viewer.ScrollableHeight;
        var viewport=horizontal?viewer.ViewportWidth:viewer.ViewportHeight;
        var running=horizontal?state.HorizontalAnimating:state.VerticalAnimating;
        var previous=running ? horizontal?state.HorizontalTarget:state.VerticalTarget : current;
        var step=Math.Min(PixelsPerNotch,Math.Max(1,viewport/4));
        var target=Math.Clamp(previous-Math.Clamp(delta/WheelNotch,-MaximumNotchesPerEvent,MaximumNotchesPerEvent)*step,0,maximum);
        var property=horizontal?HorizontalOffsetProperty:VerticalOffsetProperty;
        if(horizontal){state.HorizontalTarget=target;state.HorizontalAnimating=true;}else{state.VerticalTarget=target;state.VerticalAnimating=true;}
        if(!animate || !SystemParameters.ClientAreaAnimation)
        {
            viewer.SetValue(property,target);viewer.BeginAnimation(property,null);
            if(horizontal)state.HorizontalAnimating=false;else state.VerticalAnimating=false;
            return;
        }
        var animation=new DoubleAnimation(current,target,Duration){EasingFunction=new QuadraticEase{EasingMode=EasingMode.EaseOut}};
        animation.Completed+=(_,_)=>{if(horizontal)state.HorizontalAnimating=false;else state.VerticalAnimating=false;};
        viewer.BeginAnimation(property,animation,HandoffBehavior.SnapshotAndReplace);
    }
    private static void Stop(ScrollViewer viewer)
    {
        var state=States.GetOrCreateValue(viewer);
        viewer.SetValue(VerticalOffsetProperty,viewer.VerticalOffset);viewer.SetValue(HorizontalOffsetProperty,viewer.HorizontalOffset);
        viewer.BeginAnimation(VerticalOffsetProperty,null);viewer.BeginAnimation(HorizontalOffsetProperty,null);
        state.VerticalAnimating=state.HorizontalAnimating=false;
        state.VerticalTarget=viewer.VerticalOffset;state.HorizontalTarget=viewer.HorizontalOffset;
    }
    private static void PointerDown(object sender,MouseButtonEventArgs e)=>Stop((ScrollViewer)sender);
    private static void KeyDown(object sender,KeyEventArgs e)=>Stop((ScrollViewer)sender);
    private static void BringIntoView(object sender,RequestBringIntoViewEventArgs e)=>Stop((ScrollViewer)sender);
    private static void Unloaded(object sender,RoutedEventArgs e)=>Stop((ScrollViewer)sender);
    private static void ScrollChanged(object sender,ScrollChangedEventArgs e)
    {
        var viewer=(ScrollViewer)sender;if(!ReferenceEquals(e.OriginalSource,viewer))return;
        if(e.ExtentHeightChange!=0||e.ViewportHeightChange!=0||e.ExtentWidthChange!=0||e.ViewportWidthChange!=0)Stop(viewer);
        var state=States.GetOrCreateValue(viewer);
        if(!state.VerticalAnimating)state.VerticalTarget=viewer.VerticalOffset;
        if(!state.HorizontalAnimating)state.HorizontalTarget=viewer.HorizontalOffset;
    }
}

