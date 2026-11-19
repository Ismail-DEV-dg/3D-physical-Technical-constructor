using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CircuitEditor.App.Controls;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Views;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Validation;
using CircuitEditor.Infrastructure.Repositories;
namespace CircuitEditor.Tests;
internal static class PanelPresentationChecks
{
    internal static void Run(ResourceDictionary resources)
    {
        Application.Current.Resources.MergedDictionaries.Add(resources);
        var navigation=new ResourceDictionary {Source=new Uri("pack://application:,,,/CircuitEditor.App;component/Resources/Navigation.xaml")};
        Application.Current.Resources.MergedDictionaries.Add(navigation);
        using var e=new ElectricalTestCircuit();var uno=e.Add("arduino-uno");var r=e.Add("resistor");
        e.Wire(uno,"5V",r,"1");e.Wire(r,"2",uno,"GND");e.Project.Refresh();
        var repo=new JsonElectricalLibraryRepository();
        using var analysis=new ProjectAnalysisViewModel(e.Project,new ErcService(repo,[]),new TestDebounceService(),e.Environment.Navigation,new DcSolver(repo));
        using var vm=new MainViewModel(e.Environment.Navigation,new ThemeService(new TestThemeResources()),new TestWindowService(),e.Environment.Session,analysis:analysis);
        var window=new CircuitEditor.App.MainWindow {DataContext=vm,Left=-10000,Top=-10000,ShowInTaskbar=false};
        try {
        window.Show();window.UpdateLayout();
        var layout=(EditorLayoutView)window.FindName("EditorLayout");
        var column=(ColumnDefinition)layout.FindName("PaletteColumn");
        var right=(ColumnDefinition)layout.FindName("InspectorColumn");
        var row=(RowDefinition)layout.FindName("AnalysisRow");
        var splitters=Descendants<GridSplitter>(layout).ToArray();Assert.Equal(3,splitters.Length);
        Assert.Equal(224,column.ActualWidth,2);
        var keyboardResize=new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(window),0,Key.Right){RoutedEvent=Keyboard.KeyDownEvent};
        splitters[0].RaiseEvent(keyboardResize);window.UpdateLayout();
        Assert.True(column.ActualWidth>224);Assert.Equal(column.ActualWidth,vm.Panels.Components.Size,2);
        var resized=column.ActualWidth;
        splitters[2].RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(window),0,Key.Left){RoutedEvent=Keyboard.KeyDownEvent});
        splitters[1].RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(window),0,Key.Up){RoutedEvent=Keyboard.KeyDownEvent});
        window.UpdateLayout();Assert.True(right.ActualWidth>280);Assert.True(row.ActualHeight>280);
        Assert.Equal(right.ActualWidth,vm.Panels.Inspector.Size,2);Assert.Equal(row.ActualHeight,vm.Panels.Analysis.Size,2);
        vm.Panels.Inspector.Reset();vm.Panels.Analysis.Reset();window.UpdateLayout();
        vm.Panels.Components.TogglePinCommand.Execute(null);window.UpdateLayout();
        Assert.Equal(28,column.ActualWidth,2);
        var pane=(DockPane)layout.FindName("PalettePane");
        Assert.Equal(Visibility.Visible,((Button)pane.Template.FindName("Rail",pane)).Visibility);
        vm.Panels.Components.ShowCommand.Execute(null);window.UpdateLayout();Assert.Equal(resized,column.ActualWidth,2);
        vm.Panels.Inspector.HideCommand.Execute(null);window.UpdateLayout();Assert.Equal(0,right.ActualWidth,2);
        analysis.CalculateCommand.Execute(null);window.UpdateLayout();Assert.Equal(0,right.ActualWidth,2);Assert.Equal(1,vm.Panels.ToolTabIndex);
        Assert.Single(Descendants<CalculationResultsView>(layout));
        Assert.IsType<PropertiesView>(((DockPane)layout.FindName("InspectorPane")).Content);
        var results=Descendants<CalculationResultsView>(layout).Single();
        Assert.Equal(analysis.Results.Entries.Count,Descendants<ListBox>(results).Single().Items.Count);
        var file=Environment.GetEnvironmentVariable("CIRCUIT_EDITOR_PANELS_REVIEW");
        if(!string.IsNullOrWhiteSpace(file))
        {
            var bitmap=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(window);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(file);encoder.Save(stream);
        }
        vm.Panels.Analysis.TogglePinCommand.Execute(null);window.UpdateLayout();Assert.Equal(28,row.ActualHeight,2);
        vm.Panels.Analysis.HideCommand.Execute(null);window.UpdateLayout();Assert.Equal(0,row.ActualHeight,2);
        vm.Panels.ResetCommand.Execute(null);window.UpdateLayout();Assert.Equal(280,row.ActualHeight,2);Assert.Equal(224,column.ActualWidth,2);
        vm.Panels.Components.Size=450;vm.Panels.Inspector.Size=500;vm.Panels.Analysis.Size=400;
        window.Width=1120;window.Height=680;window.UpdateLayout();
        var grid=(Grid)layout.FindName("EditorGrid");var center=(Grid)layout.FindName("CenterGrid");
        Assert.True(column.ActualWidth+right.ActualWidth+320+12<=grid.ActualWidth+1);
        Assert.True(row.ActualHeight+160+6<=center.ActualHeight+1);
        var tabs=Descendants<TabControl>((DockPane)layout.FindName("AnalysisPane")).First();
        Assert.Single(tabs.Items.Cast<TabItem>().Select(t=>t.TranslatePoint(new Point(),tabs).Y).Distinct());
        Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary {Source=new Uri("pack://application:,,,/CircuitEditor.App;component/Resources/Themes/LightTech.xaml")});
        window.UpdateLayout();
        Assert.Equal(Color.FromRgb(0xED,0xF2,0xF8),((SolidColorBrush)window.Background).Color);
        Application.Current.Resources.MergedDictionaries.RemoveAt(Application.Current.Resources.MergedDictionaries.Count-1);
        } finally { window.Close(); }
        Application.Current.Resources.MergedDictionaries.Remove(navigation);
        Application.Current.Resources.MergedDictionaries.Remove(resources);
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T:DependencyObject
    {
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)
        {
            var child=VisualTreeHelper.GetChild(root,i);
            if(child is T match)yield return match;
            foreach(var nested in Descendants<T>(child))yield return nested;
        }
    }
}
