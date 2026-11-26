namespace CircuitEditor.Core.Repositories;

/// <summary>Сохраняет пользовательские сочетания клавиш отдельно от проектов.</summary>
public interface IShortcutSettingsRepository
{
    /// <summary>Загружает значения, заданные пользователем.</summary>
    IReadOnlyDictionary<string, string> Load();

    /// <summary>Сохраняет все изменения или возвращает сообщение об ошибке записи.</summary>
    string? Save(IReadOnlyDictionary<string, string> overrides);
}
