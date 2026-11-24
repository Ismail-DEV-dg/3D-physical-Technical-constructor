using System.Windows;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Repositories;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Services;
using CircuitEditor.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace CircuitEditor.App;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var app = new App();
        app.InitializeComponent();
        // Composition root only: views and WPF lifetime stay outside ViewModels.
        var registrations = new ServiceCollection();
        registrations.AddSingleton<IThemeResources>(new WpfThemeResources(app.Resources));
        registrations.AddSingleton<IThemeService, ThemeService>();
        registrations.AddSingleton<INavigationService, NavigationService>();
        registrations.AddSingleton<IWindowService, WindowService>();
        registrations.AddSingleton<IComponentLibraryRepository, JsonComponentLibraryRepository>();
        registrations.AddSingleton<IBreadboardRepository, JsonBreadboardRepository>();
        registrations.AddSingleton<IFootprintRepository, JsonFootprintRepository>();
        registrations.AddSingleton<ISceneCatalogRepository, JsonSceneCatalogRepository>();
        registrations.AddSingleton<CircuitEditor.Core.Scene.ISceneBuilder, CircuitEditor.Core.Scene.SceneBuilder>();
        registrations.AddSingleton<Controls.ThreeD.ISceneMeshFactory, Controls.ThreeD.SceneMeshFactory>();
        registrations.AddSingleton<Controls.ThreeD.SceneExportAdapter>();
        registrations.AddSingleton<IThreeDExportService>(provider => provider.GetRequiredService<Controls.ThreeD.SceneExportAdapter>());
        registrations.AddSingleton<Controls.ThreeD.ISceneViewportRegistration>(provider => provider.GetRequiredService<Controls.ThreeD.SceneExportAdapter>());
        registrations.AddSingleton<ISceneBenchmark, Controls.ThreeD.SceneBenchmark>();
        registrations.AddSingleton<IStage10Benchmark, Controls.ThreeD.Stage10Benchmark>();
        registrations.AddSingleton<ILogicFamilyRepository, JsonLogicFamilyRepository>();
        registrations.AddSingleton<IChipCsvRepository, FileChipCsvRepository>();
        registrations.AddSingleton<IChipRepository, JsonChipRepository>();
        registrations.AddSingleton<IChipTemplateRepository, JsonChipTemplateRepository>();
        registrations.AddSingleton<IUserChipLibraryService, UserChipLibraryService>();
        registrations.AddSingleton<IProjectChipVersionSelector, ProjectChipVersionSelector>();
        registrations.AddSingleton<CircuitEditor.Core.Chips.IChipEvaluator, CircuitEditor.Core.Chips.ChipEvaluator>();
        registrations.AddSingleton<IErrorLogRepository, ErrorLogRepository>();
        registrations.AddSingleton<IClipboardService, ClipboardService>();
        registrations.AddSingleton<IErrorHelpRepository, JsonErrorHelpRepository>();
        registrations.AddSingleton<IGuideRepository, JsonGuideRepository>();
        registrations.AddSingleton<IErrorDocumentationService, ErrorDocumentationService>();
        registrations.AddSingleton<IErrorHelpService, ErrorHelpService>();
        registrations.AddSingleton<IElectricalLibraryRepository, JsonElectricalLibraryRepository>();
        registrations.AddSingleton<CircuitEditor.Core.Netlist.INetlistBuilder, CircuitEditor.Core.Netlist.NetlistBuilder>();
        registrations.AddSingleton<CircuitEditor.Core.Validation.IErcService, CircuitEditor.Core.Validation.ErcService>();
        registrations.AddSingleton<CircuitEditor.Core.Validation.IErcRule, CircuitEditor.Core.Validation.SupplyShortRule>();
        registrations.AddSingleton<CircuitEditor.Core.Validation.IErcRule, CircuitEditor.Core.Validation.SourceConflictRule>();
        registrations.AddSingleton<CircuitEditor.Core.Validation.IErcRule, CircuitEditor.Core.Validation.BatteryShortRule>();
        registrations.AddSingleton<CircuitEditor.Core.Validation.IErcRule, CircuitEditor.Core.Validation.EspOvervoltageRule>();
        registrations.AddSingleton<CircuitEditor.Core.Validation.IErcRule, CircuitEditor.Core.Validation.DisconnectedSupplyRule>();
        registrations.AddSingleton<CircuitEditor.Core.Validation.IErcRule, CircuitEditor.Core.Validation.ChipSupplyRule>();
        registrations.AddSingleton<CircuitEditor.Core.Validation.IErcRule, CircuitEditor.Core.Validation.RequiredPinRule>();
        registrations.AddSingleton<CircuitEditor.Core.Validation.IErcRule, CircuitEditor.Core.Validation.I2cAddressRule>();
        registrations.AddSingleton<CircuitEditor.Core.Validation.IErcRule, CircuitEditor.Core.Validation.LedResistorRule>();
        registrations.AddSingleton<CircuitEditor.Core.Validation.IErcRule, CircuitEditor.Core.Validation.LedShortRule>();
        registrations.AddSingleton<CircuitEditor.Core.Validation.IErcRule, CircuitEditor.Core.Validation.PinDirectionRule>();
        registrations.AddSingleton<IComponentInsertionService, ComponentInsertionService>();
        registrations.AddSingleton<ITextMeasurer, WpfTextMeasurer>();
        registrations.AddSingleton<IComponentLayoutCalculator, ComponentLayoutCalculator>();
        registrations.AddSingleton<IProjectSampleRepository, JsonProjectSampleRepository>();
        registrations.AddSingleton<IProjectRepository, JsonProjectRepository>();
        registrations.AddSingleton<CircuitEditor.Core.Documents.IProjectMigration, CircuitEditor.Infrastructure.Documents.ProjectMigrationV1ToV2>();
        registrations.AddSingleton<CircuitEditor.Core.Documents.IProjectMigration, CircuitEditor.Infrastructure.Documents.ProjectMigrationV2ToV3>();
        registrations.AddSingleton<CircuitEditor.Core.Documents.IProjectMigration, CircuitEditor.Infrastructure.Documents.ProjectMigrationV3ToV4>();
        registrations.AddSingleton<ISketchRepository, FileSketchRepository>();
        registrations.AddSingleton<ICodeCompletionRepository, JsonCodeCompletionRepository>();
        registrations.AddSingleton<ICodeTemplateRepository, JsonCodeTemplateRepository>();
        registrations.AddSingleton<CircuitEditor.Core.Code.ICodeGenerator, CircuitEditor.Core.Code.ArduinoCodeGenerator>();
        registrations.AddSingleton<CircuitEditor.Core.Code.ICodeAnalyzer, CircuitEditor.Core.Code.ArduinoCodeAnalyzer>();
        registrations.AddSingleton<ICodeAnalysisService, CodeAnalysisService>();
        registrations.AddSingleton<IRecentProjectsRepository, JsonRecentProjectsRepository>();
        registrations.AddSingleton<IExportRepository, FileExportRepository>();
        registrations.AddSingleton<IDialogService, DialogService>();
        registrations.AddSingleton<IAutosaveService, AutosaveService>();
        registrations.AddSingleton<IImageExportService, ImageExportService>();
        registrations.AddSingleton<IChipPinoutRenderer, ChipPinoutRenderer>();
        registrations.AddSingleton<ISvgExportService, SvgExportService>();
        registrations.AddSingleton<CircuitEditor.Core.Export.IBomExportService, CircuitEditor.Core.Export.BomExportService>();
        registrations.AddSingleton<ProjectSessionViewModel>();
        registrations.AddSingleton<ExportViewModel>();
        registrations.AddSingleton<CircuitEditor.Core.Electronics.IDcSolver, CircuitEditor.Core.Electronics.DcSolver>();
        registrations.AddSingleton<IShortcutGuideService, ShortcutGuideService>();
        registrations.AddSingleton<ICalculatorService, CalculatorService>();
        registrations.AddSingleton<CalculatorViewModel>();
        registrations.AddSingleton<IDebounceService, DebounceService>();
        registrations.AddSimulation();
        registrations.AddSingleton<ProjectAnalysisViewModel>();
        registrations.AddSingleton<ProjectViewModel>();
        registrations.AddSingleton<BreadboardViewModel>();
        registrations.AddSingleton<SchematicViewModel>();
        registrations.AddSingleton<ChipViewModel>();
        registrations.AddSingleton<ThreeDViewModel>();
        registrations.AddSingleton<CodeViewModel>();
        registrations.AddSingleton<MainViewModel>();
        registrations.AddSingleton(provider => new MainWindow
        {
            DataContext = provider.GetRequiredService<MainViewModel>()
        });
        using var services = registrations.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        app.DispatcherUnhandledException += (_, failure) =>
        {
            if (failure.Exception is OutOfMemoryException)
                return;

            services.GetRequiredService<MainViewModel>().StatusMessage =
                "Операция не выполнена. Подробности записаны в журнал.";
            services.GetRequiredService<IErrorLogRepository>().TryAppend(failure.Exception.ToString());
            failure.Handled = true;
        };
        app.Resources[typeof(Controls.ThreeD.ISceneMeshFactory)] = services.GetRequiredService<Controls.ThreeD.ISceneMeshFactory>();
        app.Resources[typeof(Controls.ThreeD.ISceneViewportRegistration)] =
            services.GetRequiredService<Controls.ThreeD.ISceneViewportRegistration>();
        if (args is ["--benchmark-stage10", var benchmark])
        {
            app.Run(services.GetRequiredService<IStage10Benchmark>().CreateWindow(benchmark));
            return;
        }
        if (args is ["--benchmark-3d", var report])
        {
            app.Run(services.GetRequiredService<ISceneBenchmark>().CreateWindow(report));
            return;
        }
        var main = services.GetRequiredService<MainWindow>();
        var session = services.GetRequiredService<ProjectSessionViewModel>();
        main.ContentRendered += (_, _) => session.Initialize();
        main.Closing += (_, args) => args.Cancel = !session.RequestClose();
        app.Run(main);
    }

}
