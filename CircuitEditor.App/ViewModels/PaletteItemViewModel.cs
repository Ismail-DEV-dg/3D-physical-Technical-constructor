using CircuitEditor.Core.Models;

namespace CircuitEditor.App.ViewModels;

public sealed class PaletteItemViewModel(ComponentDefinition definition)
{
    public ComponentDefinition Definition { get; } = definition;
    public string Name => Definition.Id switch
    {
        "arduino-uno" or "arduino-nano" or "esp32-devkit" => $"Плата {Definition.Name}",
        "lcd1602" or "lcd1602-i2c" or "oled-ssd1306" => $"Дисплей {Definition.Name}",
        "dht11" or "dht22" => $"Датчик температуры и влажности {Definition.Name}",
        "hc-sr04" => "Ультразвуковой датчик HC-SR04",
        "pir" => "Датчик движения HC-SR501",
        "rgb-led" => "Трёхцветный светодиод RGB",
        "inductor" => "Катушка индуктивности",
        _ => Definition.Name
    };
    public Uri? Thumbnail => StockComponents.Contains(Definition.Id)
        ? new($"pack://application:,,,/CircuitEditor.App;component/Resources/Components/{Definition.Id}.jpg") : null;

    private static readonly HashSet<string> StockComponents =
    [
        "buzzer", "servo", "relay", "arduino-uno", "arduino-nano", "esp32-devkit",
        "lcd1602", "lcd1602-i2c", "oled-ssd1306", "seven-segment", "resistor", "capacitor",
        "inductor", "potentiometer", "photoresistor", "thermistor", "led", "rgb-led",
        "npn", "diode", "dht11", "dht22", "hc-sr04", "pir",
        "button", "switch", "battery",
    ];
    public string Category => Definition.Category;
    public string Symbol => Definition.Prefix;
    public string Description => Definition.Description;
}
