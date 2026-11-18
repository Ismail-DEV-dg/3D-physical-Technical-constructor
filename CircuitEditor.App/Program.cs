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
        registrations.AddSingleton<IComponentInsertionService, ComponentInsertionService>();
        registrations.AddSingleton<ITextMeasurer, WpfTextMeasurer>();
        registrations.AddSingleton<IComponentLayoutCalculator, ComponentLayoutCalculator>();
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
        app.Run(services.GetRequiredService<MainWindow>());
    }

}
