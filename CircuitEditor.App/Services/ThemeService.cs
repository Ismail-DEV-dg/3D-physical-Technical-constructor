using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CircuitEditor.App.Services;

public sealed class ThemeService(IThemeResources resources) : ObservableObject, IThemeService
{
    private ThemeKind currentTheme = ThemeKind.NeonDark;
    private string? errorMessage;

    public ThemeKind CurrentTheme => currentTheme;
    public string? ErrorMessage => errorMessage;

    public bool SetTheme(ThemeKind theme)
    {
        if (!Enum.IsDefined(theme))
            return Fail("Неизвестная тема.");
        if (theme == currentTheme)
        {
            SetProperty(ref errorMessage, null, nameof(ErrorMessage));
            return true;
        }

        try
        {
            resources.Apply(theme);
            SetProperty(ref currentTheme, theme, nameof(CurrentTheme));
            SetProperty(ref errorMessage, null, nameof(ErrorMessage));
            return true;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException
                                         or System.Windows.Markup.XamlParseException or ArgumentException)
        {
            return Fail("Не удалось загрузить тему. Текущая тема сохранена.");
        }
    }

    private bool Fail(string message)
    {
        SetProperty(ref errorMessage, message, nameof(ErrorMessage));
        return false;
    }
}
