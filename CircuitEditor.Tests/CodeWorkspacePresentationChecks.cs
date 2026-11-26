using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CircuitEditor.App.Interaction;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Views;
using CircuitEditor.Core.Code;
using CircuitEditor.Infrastructure.Repositories;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
namespace CircuitEditor.Tests;
internal static class CodeWorkspacePresentationChecks
{
    internal static void Run(ResourceDictionary resources)
    {
        Application.Current.Resources.MergedDictionaries.Add(resources);
        var navigationResources=new ResourceDictionary {Source=new Uri("pack://application:,,,/CircuitEditor.App;component/Resources/Navigation.xaml")};
        Application.Current.Resources.MergedDictionaries.Add(navigationResources);
        using var e=new ElectricalTestCircuit();var board=e.Add("arduino-uno");var led=e.Add("led");var resistor=e.Add("resistor");var button=e.Add("button");
        e.Wire(board,"D9",resistor,"1");e.Wire(resistor,"2",led,"A");e.Wire(led,"K",board,"GND");
        e.Wire(board,"D2",button,"1");e.Wire(button,"2",board,"GND");e.Project.Refresh();
        var templates=new JsonCodeTemplateRepository();var api=new JsonCodeCompletionRepository();var themes=new ThemeService(new WpfThemeResources(resources));
        var worker=new CodeAnalysisService(new ArduinoCodeAnalyzer(api,templates),new Core.Netlist.NetlistBuilder(new JsonElectricalLibraryRepository()));
        using var code=new CodeViewModel(e.Project,api,templates,themes,analysisService:worker);
        var nav=new NavigationService(e.Environment.Mockup,e.Environment.Schematic,new ChipViewModel(),code);
        using var main=new MainViewModel(nav,themes,new TestWindowService(),e.Environment.Session);
        var window=new CircuitEditor.App.MainWindow {DataContext=main,Left=-10000,Top=-10000,ShowInTaskbar=false};
        var oldContext=SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(window.Dispatcher));
            main.SelectedWorkspace=code;window.Show();window.UpdateLayout();window.Dispatcher.Invoke(()=>{},DispatcherPriority.Loaded);
            var view=Find<CodeView>(window).Single();var editor=(TextEditor)view.FindName("Editor");
            code.ControllerId=board.Model.Id;
            code.Text="void setup() {\n  pinMode(9, OUTPUT);\n}\n\nvoid loop() {\n  digitalWrite(9, HIGH);\n}\n";window.UpdateLayout();
            Assert.Contains("pinMode(9, OUTPUT);",editor.Text);var count=e.Project.History.Count;
            editor.Document.Insert(editor.Document.TextLength,"\n// manual extension\nvoid testPin() { digitalWrite(8,HIGH); }\n");
            Assert.Equal(board.Model.Id,code.ControllerId);
            window.UpdateLayout();Assert.Equal(count,e.Project.History.Count);Assert.True(main.UndoActiveCommand.CanExecute(null));
            main.UndoActiveCommand.Execute(null);Assert.DoesNotContain("manual extension",code.Text);main.RedoActiveCommand.Execute(null);Assert.Contains("manual extension",code.Text);
            code.ProblemsPanel.SetAnalysis(new ArduinoCodeAnalyzer(api,templates).Analyze(e.Project.Model,e.Project.Connectivity.Netlist,code.Sketch));
            var problem=code.ProblemsPanel.Items.First(p=>p.Problem.Message.Contains("D8"));
            main.SelectedWorkspace=e.Environment.Mockup;window.UpdateLayout();code.ProblemsPanel.Selected=problem;
            window.UpdateLayout();window.Dispatcher.Invoke(()=>{},DispatcherPriority.Loaded);
            Assert.Same(code,main.SelectedWorkspace);Assert.Equal(0,main.Panels.ToolTabIndex);
            view=Find<CodeView>(window).Single();editor=(TextEditor)view.FindName("Editor");Assert.Equal(problem.Problem.Line,editor.TextArea.Caret.Line);
            var original=editor.Text;editor.Document.Insert(editor.Document.TextLength,"\npin");editor.CaretOffset=editor.Document.TextLength;window.UpdateLayout();
            using(var completion=new EditorCompletionController(editor,code))
            {
                completion.Open(true);Assert.NotNull(completion.Popup);
                Assert.Contains(completion.Popup!.CompletionList.CompletionData,d=>d.Text=="pinMode");
                completion.Popup.UpdateLayout();Save(completion.Popup,"CIRCUIT_EDITOR_COMPLETION_REVIEW");completion.Dismiss();
            }
            code.Text=original;window.UpdateLayout();
            var frame=new DispatcherFrame();var timer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(750)};
            timer.Tick+=(_,_)=>{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);
            var fold=editor.TextArea.LeftMargins.OfType<FoldingMargin>().Single().FoldingManager.AllFoldings.First();
            fold.IsFolded=true;Assert.True(fold.IsFolded);fold.IsFolded=false;window.UpdateLayout();
            Assert.False(code.IsAnalyzing);Assert.Contains(code.ProblemsPanel.Items,p=>p.Problem.Message.Contains("D8"));
            Assert.DoesNotContain(code.Output.Lines,l=>l.Contains("Анализ кода"));
            Save(window,"CIRCUIT_EDITOR_CODE_REVIEW");
            themes.SetTheme(ThemeKind.LightTech);window.UpdateLayout();Save(window,"CIRCUIT_EDITOR_CODE_LIGHT_REVIEW");
            themes.SetTheme(ThemeKind.NeonDark);
        }
        finally
        {
            window.Close();Application.Current.Resources.MergedDictionaries.Remove(navigationResources);
            Application.Current.Resources.MergedDictionaries.Remove(resources);
            SynchronizationContext.SetSynchronizationContext(oldContext);
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
        {var child=VisualTreeHelper.GetChild(root,i);if(child is T value)yield return value;foreach(var nested in Find<T>(child))yield return nested;}
    }
}
