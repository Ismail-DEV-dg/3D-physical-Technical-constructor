using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CircuitEditor.App.Interaction;
namespace CircuitEditor.Tests;
internal static class SmoothScrollPresentationChecks
{
    internal static void Run(ResourceDictionary resources)
    {
        Application.Current.Resources.MergedDictionaries.Add(resources);
        var content=new Border{Height=2000,Width=300};
        var viewer=new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
        Layout(viewer);Assert.True(SmoothScrollBehavior.GetEnabled(viewer));Assert.False(viewer.CanContentScroll);
        SmoothScrollBehavior.Scroll(viewer,-120,animate:false);viewer.UpdateLayout();Assert.Equal(40,viewer.VerticalOffset,3);
        SmoothScrollBehavior.Scroll(viewer,-30,animate:false);viewer.UpdateLayout();Assert.Equal(50,viewer.VerticalOffset,3);
        SmoothScrollBehavior.Scroll(viewer,120,animate:false);viewer.UpdateLayout();Assert.Equal(10,viewer.VerticalOffset,3);
        SmoothScrollBehavior.Scroll(viewer,int.MaxValue,animate:false);viewer.UpdateLayout();Assert.Equal(0,viewer.VerticalOffset);
        var inner=new ScrollViewer{Height=120,Content=new Border{Height=1200,Width=200}};
        var panel=new StackPanel();panel.Children.Add(inner);panel.Children.Add(new Border{Height=1200});
        var outer=new ScrollViewer{Content=panel};Layout(outer);inner.UpdateLayout();
        var wheel=new MouseWheelEventArgs(Mouse.PrimaryDevice,0,-120){RoutedEvent=UIElement.PreviewMouseWheelEvent};
        inner.RaiseEvent(wheel);Assert.True(wheel.Handled);Pump();inner.UpdateLayout();outer.UpdateLayout();
        Assert.InRange(inner.VerticalOffset,29,33);Assert.Equal(0,outer.VerticalOffset);
        inner.ScrollToEnd();inner.UpdateLayout();
        var boundaryWheel=new MouseWheelEventArgs(Mouse.PrimaryDevice,0,-120){RoutedEvent=UIElement.PreviewMouseWheelEvent};
        inner.RaiseEvent(boundaryWheel);Pump();outer.UpdateLayout();Assert.InRange(outer.VerticalOffset,39,41);
        var horizontal=new ScrollViewer{Content=new Border{Width=2000,Height=100},HorizontalScrollBarVisibility=ScrollBarVisibility.Auto};
        Layout(horizontal);SmoothScrollBehavior.Scroll(horizontal,-120,horizontal:true,animate:false);horizontal.UpdateLayout();Assert.Equal(40,horizontal.HorizontalOffset,3);
        SmoothScrollBehavior.SetEnabled(horizontal,false);
        var list=new ListBox{Height=200,Resources=resources,Style=(Style)resources[typeof(ListBox)]};list.ApplyTemplate();for(var i=0;i<40;i++)list.Items.Add("Строка "+i);
        Layout(list);var scroll=Find(list);Assert.NotNull(scroll);Assert.True(SmoothScrollBehavior.GetEnabled(scroll!));Assert.False(scroll!.CanContentScroll);
        SmoothScrollBehavior.Scroll(viewer,-120);SmoothScrollBehavior.Scroll(viewer,120);Pump();viewer.UpdateLayout();Assert.InRange(viewer.VerticalOffset,0,1);
        viewer.ScrollToEnd();viewer.UpdateLayout();SmoothScrollBehavior.Scroll(viewer,-120,animate:false);viewer.UpdateLayout();Assert.Equal(viewer.ScrollableHeight,viewer.VerticalOffset,3);
        SmoothScrollBehavior.SetEnabled(viewer,false);SmoothScrollBehavior.SetEnabled(inner,false);SmoothScrollBehavior.SetEnabled(outer,false);
        Application.Current.Resources.MergedDictionaries.Remove(resources);
    }
    private static void Layout(FrameworkElement e){e.Measure(new Size(320,200));e.Arrange(new Rect(0,0,320,200));e.UpdateLayout();}
    private static ScrollViewer? Find(DependencyObject parent)
    {
        if(parent is ScrollViewer v)return v;
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=Find(VisualTreeHelper.GetChild(parent,i));if(child is not null)return child;}
        return null;
    }
    private static void Pump()
    {
        var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(260)};
        timer.Tick+=(_,_)=>{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);
    }
}

