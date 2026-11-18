using System.ComponentModel;

namespace CircuitEditor.App.Services;

public interface IThemeService : INotifyPropertyChanged
{
    ThemeKind CurrentTheme { get; }
    string? ErrorMessage { get; }
    bool SetTheme(ThemeKind theme);
}

