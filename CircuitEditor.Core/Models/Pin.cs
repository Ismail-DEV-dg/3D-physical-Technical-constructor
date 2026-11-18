namespace CircuitEditor.Core.Models;

/// <summary>Электрическое описание вывода; координаты находятся в контактах корпуса и раскладке символа.</summary>
public sealed class Pin
{
    /// <summary>Создаёт вывод с уникальным в пределах компонента идентификатором.</summary>
    public Pin(string id, string name, PinType type,
        bool isInputOnly = false, bool isExposed = true, IReadOnlyList<PinType>? capabilities = null, bool isNoConnect = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!Enum.IsDefined(type)) throw new ArgumentException("Некорректный тип вывода.");
        var functions = capabilities?.ToArray() ?? [];
        if (functions.Any(value => !Enum.IsDefined(value)))
            throw new ArgumentException("Неизвестная функция вывода.");
        Id = id; Name = name; Type = type;
        IsInputOnly = isInputOnly; IsExposed = isExposed;
        IsNoConnect = isNoConnect;
        Capabilities = Array.AsReadOnly(functions);
    }

    /// <summary>Идентификатор вывода.</summary>
    public string Id { get; }
    /// <summary>Подпись на символе.</summary>
    public string Name { get; }
    /// <summary>Основная функция.</summary>
    public PinType Type { get; }
    /// <summary>Дополнительные функции.</summary>
    public IReadOnlyList<PinType> Capabilities { get; }
    /// <summary>Вывод допускает только ввод.</summary>
    public bool IsInputOnly { get; }
    /// <summary>Доступен ли контакт на разъёме выбранной платы.</summary>
    public bool IsExposed { get; }
    /// <summary>Физически присутствующий, но внутренне неподключённый NC-контакт.</summary>
    public bool IsNoConnect { get; }
}
