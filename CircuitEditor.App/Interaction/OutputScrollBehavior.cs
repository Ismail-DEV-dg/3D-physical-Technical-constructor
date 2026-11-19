using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using CircuitEditor.App.ViewModels;
namespace CircuitEditor.App.Interaction;
public static class OutputScrollBehavior
{
    public static readonly DependencyProperty ModelProperty=DependencyProperty.RegisterAttached("Model",typeof(OutputViewModel),typeof(OutputScrollBehavior),new PropertyMetadata(null,Changed));
    public static void SetModel(DependencyObject o,OutputViewModel value)=>o.SetValue(ModelProperty,value);
    public static OutputViewModel? GetModel(DependencyObject o)=>(OutputViewModel?)o.GetValue(ModelProperty);
    private static readonly DependencyProperty HandlerProperty=DependencyProperty.RegisterAttached("Handler",typeof(NotifyCollectionChangedEventHandler),typeof(OutputScrollBehavior));
    private static void Changed(DependencyObject o,DependencyPropertyChangedEventArgs e)
    {
        if(o is not ListBox list)return;Detach(list,e.OldValue as OutputViewModel);
        list.Loaded-=Loaded;list.Unloaded-=Unloaded;
        if(e.NewValue is OutputViewModel){list.Loaded+=Loaded;list.Unloaded+=Unloaded;if(list.IsLoaded)Attach(list);}
    }
    private static void Loaded(object sender,RoutedEventArgs e)=>Attach((ListBox)sender);
    private static void Unloaded(object sender,RoutedEventArgs e)=>Detach((ListBox)sender,GetModel((ListBox)sender));
    private static void Attach(ListBox list)
    {
        if(GetModel(list) is not { } model||list.GetValue(HandlerProperty) is not null)return;
        NotifyCollectionChangedEventHandler handler=(_,_)=>{if(model.AutoScroll&&model.Lines.Count>0)list.ScrollIntoView(model.Lines[^1]);};
        list.SetValue(HandlerProperty,handler);model.Lines.CollectionChanged+=handler;
        list.PreviewMouseWheel+=Wheel;list.PreviewMouseDown+=MouseDown;
        handler(null,new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
    private static void Detach(ListBox list,OutputViewModel? model)
    {
        if(list.GetValue(HandlerProperty) is NotifyCollectionChangedEventHandler handler&&model is not null)model.Lines.CollectionChanged-=handler;
        list.ClearValue(HandlerProperty);list.PreviewMouseWheel-=Wheel;list.PreviewMouseDown-=MouseDown;
    }
    private static void Wheel(object sender,MouseWheelEventArgs e){if(e.Delta>0&&GetModel((ListBox)sender) is { } model)model.AutoScroll=false;}
    private static void MouseDown(object sender,MouseButtonEventArgs e)
    {
        var current=e.OriginalSource as DependencyObject;
        while(current is not null&&current!=sender){if(current is ScrollBar){if(GetModel((ListBox)sender) is { } model)model.AutoScroll=false;break;}current=current is Visual?VisualTreeHelper.GetParent(current):LogicalTreeHelper.GetParent(current);}
    }
}
