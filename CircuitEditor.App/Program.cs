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
    public static void Main()
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
        registrations.AddSingleton<CircuitEditor.Core.Validation.IErcRule, CircuitEditor.Core.Validation.RequiredPinRule>();
        registrations.AddSingleton<CircuitEditor.Core.Validation.IErcRule, CircuitEditor.Core.Validation.I2cAddressRule>();
        registrations.AddSingleton<CircuitEditor.Core.Validation.IErcRule, CircuitEditor.Core.Validation.LedResistorRule>();
        registrations.AddSingleton<CircuitEditor.Core.Validation.IErcRule, CircuitEditor.Core.Validation.LedShortRule>();
        registrations.AddSingleton<CircuitEditor.Core.Validation.IErcRule, CircuitEditor.Core.Validation.PinDirectionRule>();
        registrations.AddSingleton<IComponentInsertionService, ComponentInsertionService>();
        registrations.AddSingleton<ITextMeasurer, WpfTextMeasurer>();
        registrations.AddSingleton<IComponentLayoutCalculator, ComponentLayoutCalculator>();
        registrations.AddSingleton<IProjectRepository, JsonProjectRepository>();
        registrations.AddSingleton<CircuitEditor.Core.Documents.IProjectMigration, CircuitEditor.Infrastructure.Documents.ProjectMigrationV1ToV2>();
        registrations.AddSingleton<CircuitEditor.Core.Documents.IProjectMigration, CircuitEditor.Infrastructure.Documents.ProjectMigrationV2ToV3>();
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

        app.DispatcherUnhandledException += (_, args) =>
        {
            if (args.Exception is OutOfMemoryException)
                return;

            services.GetRequiredService<MainViewModel>().StatusMessage =
                "Операция не выполнена. Подробности записаны в журнал.";
            services.GetRequiredService<IErrorLogRepository>().TryAppend(args.Exception.ToString());
            args.Handled = true;
        };
        var main = services.GetRequiredService<MainWindow>();
        var session = services.GetRequiredService<ProjectSessionViewModel>();
        main.ContentRendered += (_, _) => session.Initialize();
        main.Closing += (_, args) => args.Cancel = !session.RequestClose();
        app.Run(main);
    }

}
