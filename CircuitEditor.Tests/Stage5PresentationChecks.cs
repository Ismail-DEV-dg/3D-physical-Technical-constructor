using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CircuitEditor.App.Views;
using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Controls;
using CircuitEditor.Infrastructure.Repositories;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Validation;
namespace CircuitEditor.Tests;
internal static class Stage5PresentationChecks
{
    internal static void Run(ResourceDictionary resources)
    {
        Application.Current.Resources.MergedDictionaries.Add(resources);
        using var e=new ElectricalTestCircuit();var battery=e.Add("battery");var resistor=e.Add("resistor");var led=e.Add("led");
        e.Wire(battery,"+",resistor,"1");e.Wire(resistor,"2",led,"A");e.Wire(led,"K",battery,"-");e.Project.Refresh();
        var repo=new JsonElectricalLibraryRepository();
        using var analysis=new ProjectAnalysisViewModel(e.Project,new ErcService(repo,[new LedResistorRule()]),new TestDebounceService(),e.Environment.Navigation,new DcSolver(repo));
        analysis.CalculateCommand.Execute(null);analysis.Results.ShowVoltages=true;
        Render(new AnalysisView {DataContext=analysis});
        Render(new ElectricalPropertiesView {DataContext=led.ElectricalEditor});
        Render(new BreadboardSurface {Scene=e.Environment.Mockup});
        Render(new SchematicSurface {Scene=e.Environment.Schematic});
        var calculator=new CalculatorWindow {DataContext=new CalculatorViewModel(repo,e.Project)};
        Render((FrameworkElement)calculator.Content);calculator.Close();
        var guide=new ShortcutGuideWindow {DataContext=new ShortcutGuideViewModel()};Render((FrameworkElement)guide.Content);guide.Close();
        Application.Current.Resources.MergedDictionaries.Remove(resources);
    }
    private static void Render(FrameworkElement element)
    {
        element.Measure(new Size(800,450));element.Arrange(new Rect(0,0,800,450));element.UpdateLayout();
        var bitmap=new RenderTargetBitmap(800,450,96,96,PixelFormats.Pbgra32);bitmap.Render(element);Assert.Equal(800,bitmap.PixelWidth);
    }
}
