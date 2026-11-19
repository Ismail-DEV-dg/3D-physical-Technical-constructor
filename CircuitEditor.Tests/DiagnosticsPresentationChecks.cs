using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CircuitEditor.App.Controls;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Views;
using CircuitEditor.Core.Code;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Validation;
using CircuitEditor.Infrastructure.Repositories;
namespace CircuitEditor.Tests;
internal static class DiagnosticsPresentationChecks
{
    internal static void Run(ResourceDictionary resources)
    {
        Application.Current.Resources.MergedDictionaries.Add(resources);
        var navigationResources=new ResourceDictionary{Source=new Uri("pack://application:,,,/CircuitEditor.App;component/Resources/Navigation.xaml")};
        Application.Current.Resources.MergedDictionaries.Add(navigationResources);
        using var e=new ElectricalTestCircuit();e.Add("arduino-uno");var led=e.Add("led");e.Wire(led,"A",led,"K");e.Project.Refresh();
        var services=new TestDiagnosticServices();var documentation=new ErrorDocumentationService(new JsonErrorHelpRepository());
        var themes=new ThemeService(new WpfThemeResources(resources));
        using var code=new CodeViewModel(e.Project,new ArduinoCodeGenerator(new JsonCodeTemplateRepository()),new JsonCodeCompletionRepository(),new JsonCodeTemplateRepository(),themes,clipboard:services);
        var nav=new NavigationService(e.Environment.Mockup,e.Environment.Schematic,new ChipViewModel(),new ThreeDViewModel(),code);
        var electrical=new JsonElectricalLibraryRepository();
        using var analysis=new ProjectAnalysisViewModel(e.Project,new ErcService(electrical,[new LedShortRule()]),new TestDebounceService(),nav,new DcSolver(electrical));
        using var main=new MainViewModel(nav,themes,new TestWindowService(),e.Environment.Session,analysis:analysis,clipboard:services,documentation:documentation,errorHelp:services);
        var window=new CircuitEditor.App.MainWindow{DataContext=main,Left=-10000,Top=-10000,ShowInTaskbar=false};
        ToolTip? tooltip=null;ErrorHelpWindow? helpWindow=null;
        try
        {
            main.SelectedWorkspace=code;code.Text="void setup(){pinMode(13,OUTPUT);}\nvoid loop(){digitalWrite(13,HIGH);}";
            window.Show();window.UpdateLayout();window.Dispatcher.Invoke(()=>{},DispatcherPriority.Loaded);
            code.GenerateCommand.Execute(null);window.UpdateLayout();Assert.Equal(0,main.Panels.ToolTabIndex);
            Assert.Contains(main.Diagnostics.Items,d=>d.Code=="LED_SHORT");Assert.Contains(main.Diagnostics.Items,d=>d.Code=="CODE_AUTO_MARKERS");
            var layout=(EditorLayoutView)window.FindName("EditorLayout");var inspector=(DockPane)layout.FindName("InspectorPane");
            Assert.IsType<PropertiesView>(inspector.Content);Assert.Empty(Find<TabControl>(inspector));
            var tools=(DockPane)layout.FindName("AnalysisPane");var tabs=Find<TabControl>(tools).Single();
            Assert.Equal(new[]{"Проблемы","Расчёт DC","Вывод","Симуляция"},tabs.Items.Cast<TabItem>().Select(t=>t.Header.ToString()));
            var list=Find<DiagnosticsView>(tools).Single();var message=Find<TextBox>(list).First(t=>t.DataContext is DiagnosticItemViewModel {Code:"CODE_AUTO_MARKERS"});
            Assert.True(message.IsReadOnly);var row=(DiagnosticItemViewModel)message.DataContext;
            row.CopyCommand.Execute(null);Assert.Contains("CODE_AUTO_MARKERS",services.Copied!);
            tooltip=(ToolTip)message.ToolTip;tooltip.PlacementTarget=message;tooltip.IsOpen=true;window.UpdateLayout();
            Assert.Same(row,tooltip.DataContext);var articleButton=Find<Button>(tooltip).Single();Assert.Same(row.HelpCommand,articleButton.Command);
            articleButton.Command.Execute(null);Assert.Equal("CODE_AUTO_MARKERS",services.OpenedCode);tooltip.IsOpen=false;
            var helpModel=new ErrorHelpViewModel(documentation,services);helpModel.SelectCode("CODE_AUTO_MARKERS");
            helpWindow=new ErrorHelpWindow{Owner=window,DataContext=helpModel,Left=-10000,Top=-10000,ShowInTaskbar=false};helpWindow.Show();helpWindow.UpdateLayout();
            Assert.Contains(Find<TextBox>(helpWindow),t=>t.IsReadOnly&&t.Text.Contains("Подготовить"));
            helpModel.Search="A15";helpWindow.UpdateLayout();Assert.Single(helpModel.Articles);helpModel.CopyCommand.Execute(null);Assert.Contains("LED_SHORT",services.Copied!);
            Save(helpWindow,"CIRCUIT_EDITOR_ERROR_HELP_REVIEW");helpWindow.Close();helpWindow=null;
            Save(window,"CIRCUIT_EDITOR_DIAGNOSTICS_REVIEW");
            themes.SetTheme(ThemeKind.LightTech);window.UpdateLayout();Save(window,"CIRCUIT_EDITOR_DIAGNOSTICS_LIGHT_REVIEW");themes.SetTheme(ThemeKind.NeonDark);
            var manual=code.Text;code.PrepareMarkersCommand.Execute(null);Assert.Contains("digitalWrite(13,HIGH);",code.Text);
            main.UndoActiveCommand.Execute(null);Assert.Equal(manual,code.Text);main.RedoActiveCommand.Execute(null);Assert.Contains("// <auto>",code.Text);
            code.GenerateCommand.Execute(null);Assert.DoesNotContain(main.Diagnostics.Items,d=>d.Code=="CODE_AUTO_MARKERS");Assert.Contains(main.Diagnostics.Items,d=>d.Code=="LED_SHORT");
            analysis.CalculateCommand.Execute(null);window.UpdateLayout();Assert.Equal(1,main.Panels.ToolTabIndex);Assert.Single(Find<CalculationResultsView>(tools));
        }
        finally
        {
            if(tooltip is not null)tooltip.IsOpen=false;helpWindow?.Close();window.Close();
            Application.Current.Resources.MergedDictionaries.Remove(navigationResources);Application.Current.Resources.MergedDictionaries.Remove(resources);
        }
    }
    private static void Save(Window window,string variable)
    {
        var file=Environment.GetEnvironmentVariable(variable);if(string.IsNullOrWhiteSpace(file))return;
        var bitmap=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(window);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(file);encoder.Save(stream);
    }
    private static IEnumerable<T> Find<T>(DependencyObject root) where T:DependencyObject
    {
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)
        {var child=VisualTreeHelper.GetChild(root,i);if(child is T match)yield return match;foreach(var item in Find<T>(child))yield return item;}
    }
}
