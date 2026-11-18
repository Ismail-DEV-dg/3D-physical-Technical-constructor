using System.IO;
using System.Windows;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
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
            WriteError(args.Exception);
            args.Handled = true;
        };
        app.Run(services.GetRequiredService<MainWindow>());
    }

    private static void WriteError(Exception exception)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CircuitEditor");
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, "errors.log"),
                $"{DateTimeOffset.Now:O} {exception}{Environment.NewLine}");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // Logging failures must not cause a second UI exception.
        }
    }
}

