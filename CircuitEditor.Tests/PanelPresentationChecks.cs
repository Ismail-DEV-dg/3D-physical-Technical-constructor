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
        CheckStartupTimeline();
        var navigation=new ResourceDictionary {Source=new Uri("pack://application:,,,/CircuitEditor.App;component/Resources/Navigation.xaml")};
        Application.Current.Resources.MergedDictionaries.Add(navigation);
        using var e=new ElectricalTestCircuit();var uno=e.Add("arduino-uno");var r=e.Add("resistor");
        e.Wire(uno,"5V",r,"1");e.Wire(r,"2",uno,"GND");e.Project.Refresh();
        var repo=new JsonElectricalLibraryRepository();
        using var analysis=new ProjectAnalysisViewModel(e.Project,new ErcService(repo,[]),new TestDebounceService(),e.Environment.Navigation,new DcSolver(repo));
        using var playback = new SimulationPlayback(new Simulation.Engine.SimulationSessionFactory(
            new Core.Netlist.NetlistBuilder(repo), new DcSolver(repo), new JsonSimulationModelRepository(),
            new Simulation.Scenarios.ScenarioRunner(new JsonScenarioRepository()), []));
        using var simulation = new SimulationViewModel(e.Project, playback, new SimulationUiPulse(), new JsonScenarioRepository());
        using var vm=new MainViewModel(e.Environment.Navigation,new ThemeService(new TestThemeResources()),new TestWindowService(),e.Environment.Session,analysis:analysis,simulation:simulation);
        var window=new CircuitEditor.App.MainWindow {DataContext=vm,Left=-10000,Top=-10000,ShowInTaskbar=false};
        try {
        window.Show();window.UpdateLayout();
        var layout=(EditorLayoutView)window.FindName("EditorLayout");
        var column=(ColumnDefinition)layout.FindName("PaletteColumn");
        var row=(RowDefinition)layout.FindName("AnalysisRow");
        var palette = Descendants<ListBox>((DockPane)layout.FindName("PalettePane")).Single();
        var surface = Descendants<BreadboardSurface>(layout).Single();
        Assert.True(palette.IsEnabled);
        Assert.True(surface.AllowDrop);
        Assert.IsAssignableFrom<DrawingBrush>(surface.Background);
        vm.Mockup.ShowGrid = false;
        window.UpdateLayout();
        Assert.IsAssignableFrom<SolidColorBrush>(surface.Background);
        vm.Mockup.ShowGrid = true;
        window.UpdateLayout();
        Assert.All(Descendants<Image>(palette), image =>
        {
            Assert.Equal(Stretch.Uniform, image.Stretch);
            Assert.Null(image.Clip);
        });
        Assert.Equal(27, vm.Palette.Count);
        Assert.Equal(27, vm.Palette.Select(item => item.Thumbnail).Distinct().Count());
        foreach (var item in vm.Palette)
        {
            Assert.NotNull(item.Thumbnail);
            using var asset = Application.GetResourceStream(item.Thumbnail!)!.Stream;
            Assert.True(asset.Length > 0);
        }
        vm.Project.IsSimulationActive = true;
        window.UpdateLayout();
        Assert.False(palette.IsEnabled);
        Assert.False(surface.AllowDrop);
        vm.Project.IsSimulationActive = false;
        window.UpdateLayout();
        Assert.True(palette.IsEnabled);
        Assert.True(surface.AllowDrop);
        var splitters=Descendants<GridSplitter>(layout).ToArray();Assert.Equal(2,splitters.Length);
        Assert.Equal(280,column.ActualWidth,2);
        Assert.Equal(0,row.ActualHeight,2);
        var ercStatus=(Button)window.FindName("ErcStatus");
        ercStatus.Command.Execute(null); window.UpdateLayout(); Assert.True(row.ActualHeight > 0);
        ercStatus.Command.Execute(null); window.UpdateLayout(); Assert.Equal(0,row.ActualHeight,2);
        vm.SetZoomCommand.Execute("150");
        Assert.Equal(150, vm.ZoomPercent, 2);
        var keyboardResize=new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(window),0,Key.Left){RoutedEvent=Keyboard.KeyDownEvent};
        splitters.Single(splitter => splitter.ResizeDirection == GridResizeDirection.Columns).RaiseEvent(keyboardResize);window.UpdateLayout();
        Assert.True(column.ActualWidth>280);Assert.Equal(column.ActualWidth,vm.Panels.Components.Size,2);
        var resized=column.ActualWidth;
        vm.Panels.Analysis.ShowCommand.Execute(null);
        window.UpdateLayout();
        splitters.Single(splitter => splitter.ResizeDirection == GridResizeDirection.Rows).RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(window),0,Key.Up){RoutedEvent=Keyboard.KeyDownEvent});
        window.UpdateLayout();Assert.True(row.ActualHeight>280);
        Assert.Equal(row.ActualHeight,vm.Panels.Analysis.Size,2);
        vm.Panels.Inspector.Reset();vm.Panels.Analysis.Reset();window.UpdateLayout();
        vm.Panels.Components.TogglePinCommand.Execute(null);window.UpdateLayout();
        Assert.Equal(28,column.ActualWidth,2);
        var pane=(DockPane)layout.FindName("PalettePane");
        Assert.Equal(Visibility.Visible,((Button)pane.Template.FindName("Rail",pane)).Visibility);
        vm.Panels.Components.ShowCommand.Execute(null);window.UpdateLayout();Assert.Equal(resized,column.ActualWidth,2);
        vm.Panels.Inspector.HideCommand.Execute(null);window.UpdateLayout();
        analysis.CalculateCommand.Execute(null);window.UpdateLayout();Assert.Equal(1,vm.Panels.ToolTabIndex);
        Assert.Single(Descendants<CalculationResultsView>(layout));
        Assert.Single(Descendants<ContextPropertiesView>(layout));
        var results=Descendants<CalculationResultsView>(layout).Single();
        Assert.Equal(analysis.Results.Entries.Count,Descendants<ListBox>(results).Single().Items.Count);
        vm.Panels.Analysis.TogglePinCommand.Execute(null);window.UpdateLayout();Assert.Equal(0,row.ActualHeight,2);
        vm.Panels.Analysis.HideCommand.Execute(null);window.UpdateLayout();Assert.Equal(0,row.ActualHeight,2);
        vm.Panels.ResetCommand.Execute(null);window.UpdateLayout();Assert.Equal(0,row.ActualHeight,2);Assert.Equal(280,column.ActualWidth,2);
        vm.Panels.Analysis.ShowCommand.Execute(null);
        vm.Panels.Components.Size=450;vm.Panels.Inspector.Size=500;vm.Panels.Analysis.Size=400;
        window.Width=1120;window.Height=680;window.UpdateLayout();
        var grid=(Grid)layout.FindName("EditorGrid");var center=(Grid)layout.FindName("CenterGrid");
        Assert.True(column.ActualWidth+320+6<=grid.ActualWidth+1);
        Assert.True(row.ActualHeight+160+6<=center.ActualHeight+1);
        var tabs=Descendants<TabControl>((DockPane)layout.FindName("AnalysisPane")).First();
        Assert.Single(tabs.Items.Cast<TabItem>().Select(t=>t.TranslatePoint(new Point(),tabs).Y).Distinct());
        Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary {Source=new Uri("pack://application:,,,/CircuitEditor.App;component/Resources/Themes/LightTech.xaml")});
        window.UpdateLayout();
        Assert.Equal(Color.FromRgb(0xED,0xF2,0xF8),((SolidColorBrush)window.Background).Color);
        Application.Current.Resources.MergedDictionaries.RemoveAt(Application.Current.Resources.MergedDictionaries.Count-1);
        CheckCompactShell(window, vm);
        var file=Environment.GetEnvironmentVariable("CIRCUIT_EDITOR_PANELS_REVIEW");
        if(!string.IsNullOrWhiteSpace(file))
        {
            window.Width = 1440; window.Height = 900;
            var samples = new JsonProjectSampleRepository();
            var blink = samples.Load().Single(sample => sample.File == "uno-blink.circ");
            e.Environment.Decision = SaveDecision.Discard;
            e.Environment.Session.OpenRecentCommand.Execute(samples.PathFor(blink));
            Assert.Equal(3, vm.Project.ComponentCount);
            vm.Panels.Components.Reset();
            vm.Panels.Analysis.HideCommand.Execute(null);
            vm.Panels.Inspector.ShowCommand.Execute(null);
            vm.Panels.Inspector.IsOpen = false;
            vm.Project.Select(vm.Project.Components.Single(part => part.Model.Definition.Id == "resistor"));
            vm.Mockup.ShowWireLabels = true;
            vm.Mockup.ShowNetColors = true;
            window.UpdateLayout();
            vm.FitViewCommand.Execute(null);
            var frame = new System.Windows.Threading.DispatcherFrame();
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(4150) };
            timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
            timer.Start();
            System.Windows.Threading.Dispatcher.PushFrame(frame);
            window.UpdateLayout();
            Capture(window, file);
            var compact=Environment.GetEnvironmentVariable("CIRCUIT_EDITOR_COMPACT_REVIEW");
            if (!string.IsNullOrWhiteSpace(compact))
            {
                window.Width = 1000; window.Height = 600; window.UpdateLayout();
                vm.FitViewCommand.Execute(null); window.UpdateLayout();
                Capture(window, compact);
            }
        }
        } finally { window.Close(); }
        Application.Current.Resources.MergedDictionaries.Remove(navigation);
        Application.Current.Resources.MergedDictionaries.Remove(resources);
    }
    private static void CheckStartupTimeline()
    {
        var view = new StartupView { Visibility = Visibility.Visible };
        view.Measure(new Size(1000, 580));
        view.Arrange(new Rect(0, 0, 1000, 580));
        var storyboard = Assert.IsType<System.Windows.Media.Animation.BeginStoryboard>(
            Assert.IsType<EventTrigger>(Assert.Single(view.Triggers)).Actions[0]).Storyboard;
        storyboard.Begin(view, true);
        void Seek(double seconds) => storyboard.SeekAlignedToLastTick(view, TimeSpan.FromSeconds(seconds),
            System.Windows.Media.Animation.TimeSeekOrigin.BeginTime);
        var avatar = (Border)view.FindName("Avatar");
        var lettering = (StartupLettering)view.FindName("Lettering");
        var glow = Assert.IsType<System.Windows.Media.Effects.DropShadowEffect>(lettering.Effect);
        var rim = (System.Windows.Shapes.Ellipse)view.FindName("SealRim");
        var rimGlow = Assert.IsType<System.Windows.Media.Effects.DropShadowEffect>(rim.Effect);
        var aurora = (StartupAurora)view.FindName("Aurora");
        var intro = (FrameworkElement)view.FindName("Intro");

        Seek(.36);
        Assert.InRange(avatar.Opacity, .1, .99);
        Assert.Equal(0, lettering.Progress);
        Assert.Equal(1, rim.Opacity);
        var initialRimGlow = rimGlow.Opacity;
        Assert.True(initialRimGlow > 0);
        var phase = aurora.Phase;
        Seek(1.35);
        Assert.Equal(1, avatar.Opacity);
        Assert.InRange(lettering.Progress, .1, .95);
        Assert.Equal(0, glow.Opacity);
        Assert.True(aurora.Phase > phase);
        Seek(3.30);
        Assert.Equal(1, lettering.Progress);
        Assert.Equal(initialRimGlow, rimGlow.Opacity);
        Assert.InRange(glow.Opacity, .6, .95);
        Assert.Equal(1, intro.Opacity);
        var capture = Environment.GetEnvironmentVariable("CIRCUIT_EDITOR_INTRO_REVIEW");
        if (!string.IsNullOrWhiteSpace(capture))
        {
            view.UpdateLayout();
            Capture(view, capture);
        }
        Seek(4.00);
        phase = aurora.Phase;
        Seek(5.14);
        Assert.Equal(Visibility.Visible, intro.Visibility);
        Assert.Equal(1, intro.Opacity);
        Assert.Equal(.64, glow.Opacity, 6);
        Assert.True(aurora.Phase > phase);
        Seek(5.325);
        Assert.InRange(intro.Opacity, .1, .9);
        Assert.Equal(1.6225 * 5.325 / 5.5, aurora.Phase, 6);
        Seek(5.50);
        Assert.Equal(Visibility.Collapsed, intro.Visibility);
        storyboard.Remove(view);
    }
    private static void CheckCompactShell(Window window, MainViewModel vm)
    {
        window.Width = 830;
        window.Height = 520;
        vm.Panels.Components.Reset();
        vm.Panels.Analysis.HideCommand.Execute(null);
        foreach (var workspace in vm.Workspaces)
        {
            vm.SelectedWorkspace = workspace;
            window.UpdateLayout();
            var tabs = (FrameworkElement)window.FindName("WorkspaceChooser");
            var transport = (FrameworkElement)window.FindName("SimulationTransport");
            var status = (FrameworkElement)window.FindName("StatusArea");
            Assert.True(tabs.TranslatePoint(new Point(tabs.ActualWidth, 0), window).X <=
                        transport.TranslatePoint(new Point(), window).X);
            Assert.True(status.ActualWidth >= status.DesiredSize.Width - 1);
            foreach (var tab in Descendants<TabItem>(tabs))
            {
                Assert.True(tab.ActualWidth >= 72, tab.Header?.ToString());
                Assert.True(tab.ActualHeight >= 25, tab.Header?.ToString());
            }
        }
        vm.NavigateCommand.Execute(WorkspaceKind.Mockup);
        window.UpdateLayout();
        var layout = (EditorLayoutView)window.FindName("EditorLayout");
        var mockup = Descendants<BreadboardView>(layout).Single();
        var flyout = (FrameworkElement)mockup.FindName("SimulationBody");
        var viewport = (FrameworkElement)mockup.FindName("CanvasViewport");
        var footer = (TextBlock)mockup.FindName("CanvasFooter");
        Assert.True(footer.TranslatePoint(new Point(), mockup).Y >=
                    viewport.TranslatePoint(new Point(0, viewport.ActualHeight), mockup).Y);
        var height = viewport.ActualHeight;
        var width = viewport.ActualWidth;
        vm.Mockup.IsSimulationSettingsOpen = true;
        window.UpdateLayout();
        Assert.True(flyout.IsVisible);
        Assert.Equal(height, viewport.ActualHeight, 2);
        Assert.Equal(width, viewport.ActualWidth, 2);
        Assert.True(flyout.ActualWidth <= width);
        Assert.True(flyout.ActualHeight <= height);
        vm.Mockup.IsSimulationSettingsOpen = false;
    }

    private static void Capture(FrameworkElement window, string file)
    {
        var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(file);
        encoder.Save(stream);
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
