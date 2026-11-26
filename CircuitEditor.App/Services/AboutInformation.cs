using System.Reflection;
using ICSharpCode.AvalonEdit;

namespace CircuitEditor.App.Services;

internal static class AboutInformation
{
    public static string Describe()
    {
        var application = typeof(App).Assembly;
        var version = application.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            .Split('+')[0] ?? application.GetName().Version?.ToString();
        return $"""
            Circuit Editor {version}
            Редактор электрических схем для Arduino и ESP32.
            Лицензия проекта · MIT
            {application.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright}

            Основные зависимости:
            AvalonEdit {Version(typeof(TextEditor))} · MIT
            CommunityToolkit.Mvvm {Version(typeof(CommunityToolkit.Mvvm.ComponentModel.ObservableObject))} · MIT
            Microsoft.Extensions.DependencyInjection {Version(typeof(Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions))} · MIT
            JetBrains Mono · SIL Open Font License 1.1
            """;
    }

    private static string? Version(Type type) => type.Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? type.Assembly.GetName().Version?.ToString();
}
