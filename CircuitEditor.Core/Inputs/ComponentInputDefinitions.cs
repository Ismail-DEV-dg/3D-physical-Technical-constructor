namespace CircuitEditor.Core.Inputs;

/// <summary>Единые диапазоны, варианты и начальные значения датчиков и органов управления.</summary>
public static class ComponentInputDefinitions
{
    private static IReadOnlyList<T> ReadOnly<T>(params T[] values) => Array.AsReadOnly(values);
    private static readonly IReadOnlyList<InputDefinition> Dht11 = ReadOnly(
        new InputDefinition(ComponentInput.Temperature, "Температура", 0, 50, 1, 25, "°C"),
        new InputDefinition(ComponentInput.Humidity, "Влажность", 20, 90, 1, 50, "% RH"));
    private static readonly IReadOnlyList<InputDefinition> Dht22 = ReadOnly(
        new InputDefinition(ComponentInput.Temperature, "Температура", -40, 80, .1, 25, "°C"),
        new InputDefinition(ComponentInput.Humidity, "Влажность", 0, 100, .1, 50, "% RH"));
    private static readonly IReadOnlyList<InputDefinition> Distance = ReadOnly(
        new InputDefinition(ComponentInput.Distance, "Расстояние", 2, 400, 1, 100, "см"));
    private static readonly IReadOnlyList<InputDefinition> Light = ReadOnly(
        new InputDefinition(ComponentInput.Illuminance, "Освещённость", 0, 100, 1, 50, "%"));
    private static readonly IReadOnlyList<InputDefinition> Ntc = ReadOnly(
        new InputDefinition(ComponentInput.Temperature, "Температура", -55, 125, 1, 25, "°C"));
    private static readonly IReadOnlyList<InputDefinition> Motion = ReadOnly(
        new InputDefinition(ComponentInput.Motion, "Движение", 0, 1, 1, 0, "", ReadOnly(0d, 1d), ReadOnly("Нет движения", "Движение есть")));
    private static readonly IReadOnlyList<InputDefinition> Rgb = ReadOnly(
        new InputDefinition(ComponentInput.RgbCommon, "Общий вывод RGB", 0, 1, 1, 0, "", ReadOnly(0d, 1d), ReadOnly("Общий катод", "Общий анод")));
    private static readonly IReadOnlyList<InputDefinition> Switch = ReadOnly(
        new InputDefinition(ComponentInput.SwitchPosition, "Положение", 0, 1, 1, 0, "", ReadOnly(0d, 1d), ReadOnly("A ← COM", "COM → B")));
    private static readonly IReadOnlyList<InputDefinition> Battery = ReadOnly(
        new InputDefinition(ComponentInput.BatteryVoltage, "Напряжение", 1.5, 9, .1, 9, "В", ReadOnly(1.5, 3, 3.7, 4.5, 5, 9)));

    /// <summary>Возвращает только релевантные выбранному типу параметры.</summary>
    public static IReadOnlyList<InputDefinition> For(string componentId) => componentId switch
    {
        "dht11" => Dht11, "dht22" => Dht22, "hc-sr04" => Distance, "pir" => Motion,
        "photoresistor" => Light, "thermistor" => Ntc, "rgb-led" => Rgb, "switch" => Switch,
        "button" => ReadOnly(new InputDefinition(ComponentInput.ButtonPressed,"Кнопка замкнута",0,1,1,0,"",ReadOnly(0d,1d),ReadOnly("Нет","Да"))),
        "relay" => ReadOnly(new InputDefinition(ComponentInput.RelayEnergized,"Катушка реле включена (статически)",0,1,1,0,"",ReadOnly(0d,1d),ReadOnly("Нет","Да"))),
        "battery" => Battery, _ => Array.Empty<InputDefinition>()
    };
    /// <summary>Создаёт независимые начальные условия экземпляра.</summary>
    public static ComponentInputs Defaults(string componentId) => new(For(componentId).ToDictionary(item => item.Input, item => item.Default));
    /// <summary>Проверяет и значения, и точный набор применимых ключей при восстановлении JSON.</summary>
    public static bool IsValid(string componentId, ComponentInputs state)
    {
        var definitions = For(componentId);
        return state.Values.Count == definitions.Count && definitions.All(definition =>
            state.Values.TryGetValue(definition.Input, out var value) && definition.IsValid(value));
    }
    /// <summary>Нужно ли менять статичное изображение при редактировании параметра.</summary>
    public static bool ChangesAppearance(ComponentInput input) =>
        input is ComponentInput.RgbCommon or ComponentInput.SwitchPosition or ComponentInput.BatteryVoltage;
}
