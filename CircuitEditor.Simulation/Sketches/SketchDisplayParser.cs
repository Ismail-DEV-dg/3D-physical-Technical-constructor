using System.Text.Json;
using CircuitEditor.Core.Code;
using CircuitEditor.Core.Models;
using CircuitEditor.Simulation.Engine;

namespace CircuitEditor.Simulation.Sketches;

internal sealed class SketchDisplayParser(SimulationCircuit circuit, Guid controller, CodeBoardProfile board,
    Func<CppToken> next, Func<CppToken> current, Func<string, bool> take, Action<string> expect,
    Func<CppToken, BoardPinDefinition> pin, Func<CppToken, long, long, long> number, Func<SketchExpression> expression)
{
    private const int LcdColumns = 16, LcdRows = 2;
    private const int OledWidth = 128, OledHeight = 64, OledColumns = 21, OledCellWidth = 6, OledCellHeight = 8;
    private readonly Dictionary<string, (string Kind, Component? Part)> displays = new(StringComparer.Ordinal);

    internal static bool Supports(string type) => type is "LiquidCrystal_I2C" or "LiquidCrystal" or "Adafruit_SSD1306";
    internal bool Contains(string name) => displays.ContainsKey(name);

    internal void Declare(string type, CppToken name)
    {
        expect("(");
        Component? part = type switch
        {
            "LiquidCrystal_I2C" => DeclareI2cLcd(),
            "LiquidCrystal" => DeclareParallelLcd(name),
            _ => DeclareOled()
        };
        expect(")");
        if (part is not null) EnsureDistinct(name, part);
        displays.Add(name.Text, (type, part));
    }

    internal SketchInstruction? ParseCall(CppToken receiver, CppToken method)
    {
        var (kind, part) = displays[receiver.Text];
        if (kind == "Adafruit_SSD1306" && method.Text == "begin")
        {
            BeginOled(receiver);
            return null;
        }
        if (part is null) throw new SketchSyntaxException(receiver, $"Сначала вызовите {receiver.Text}.begin() с адресом OLED.");
        var oled = kind == "Adafruit_SSD1306";
        return method.Text switch
        {
            "init" or "backlight" when kind == "LiquidCrystal_I2C" => null,
            "begin" when kind == "LiquidCrystal" => BeginParallel(),
            "print" or "println" => Print(part, method.Text == "println"),
            "clear" when !oled => new(SketchOperation.DisplayClear, Device: part.Id),
            "home" when !oled => new(SketchOperation.DisplayCursor, Device: part.Id),
            "clearDisplay" when oled => new(SketchOperation.DisplayClear, Device: part.Id),
            "display" when oled => new(SketchOperation.DisplayFlush, Device: part.Id),
            "setTextSize" when oled => OledTextSize(),
            "setTextColor" when oled => OledTextColor(),
            "setCursor" => Cursor(part, oled),
            _ => throw new SketchSyntaxException(method, $"Команда дисплея «{method.Text}» недоступна в учебном запуске.")
        };
    }

    internal SketchExpression ParseExpression(CppToken receiver, CppToken method)
    {
        if (displays.GetValueOrDefault(receiver.Text).Kind != "Adafruit_SSD1306" || method.Text != "begin")
            throw new SketchSyntaxException(method, "В условии поддерживается проверка результата oled.begin().");
        BeginOled(receiver);
        return new(SketchExpressionKind.Constant, 1);
    }

    private Component DeclareI2cLcd()
    {
        var address = next();
        expect(",");
        Dimensions();
        return FindI2c(address, "lcd1602-i2c", "LCD");
    }

    private Component DeclareParallelLcd(CppToken name)
    {
        string[] terminals = ["RS", "E", "D4", "D5", "D6", "D7"];
        var gpio = new string[terminals.Length];
        for (var i = 0; i < gpio.Length; i++)
        {
            if (i != 0) expect(",");
            var argument = next();
            var output = pin(argument);
            if (output.IsInputOnly) throw new SketchSyntaxException(argument, $"Пин {output.Id} работает только на вход.");
            gpio[i] = output.Id;
        }
        var matches = circuit.Project.Circuit.Components.Where(part => part.Definition.Id == "lcd1602" &&
            terminals.Zip(gpio).All(pair => Connected(pair.Second, part, pair.First))).Take(2).ToArray();
        return matches.Length == 1 ? matches[0] : throw new SketchSyntaxException(name,
            "Подключите один параллельный LCD к пинам из конструктора в порядке RS, E, D4, D5, D6, D7.");
    }

    private Component? DeclareOled()
    {
        number(next(), OledWidth, OledWidth);
        expect(",");
        number(next(), OledHeight, OledHeight);
        expect(",");
        expect("&");
        expect("Wire");
        if (take(","))
        {
            expect("-");
            number(next(), 1, 1);
        }
        return null;
    }

    private void BeginOled(CppToken receiver)
    {
        var power = next();
        if (power.Text is not ("SSD1306_SWITCHCAPVCC" or "SSD1306_EXTERNALVCC"))
            throw new SketchSyntaxException(power, "Укажите SSD1306_SWITCHCAPVCC или SSD1306_EXTERNALVCC.");
        expect(",");
        var device = FindI2c(next(), "oled-ssd1306", "OLED");
        if (take(","))
        {
            number(next(), 0, 1);
            if (take(",")) number(next(), 0, 1);
        }
        EnsureDistinct(receiver, device);
        displays[receiver.Text] = ("Adafruit_SSD1306", device);
    }

    private SketchInstruction? BeginParallel()
    {
        Dimensions();
        return null;
    }

    private void Dimensions()
    {
        number(next(), LcdColumns, LcdColumns);
        expect(",");
        number(next(), LcdRows, LcdRows);
    }

    private SketchInstruction Print(Component part, bool newLine)
    {
        if (current().Text.StartsWith('"'))
        {
            var token = next();
            try
            {
                var text = JsonSerializer.Deserialize<string>(token.Text) ?? "";
                return new(SketchOperation.DisplayText, Value: newLine ? 1 : 0, Device: part.Id, Text: text);
            }
            catch (JsonException) { throw new SketchSyntaxException(token, "Укажите текст в двойных кавычках."); }
        }
        var value = newLine && current().Text == ")" ? null : expression();
        return new(SketchOperation.DisplayText, Value: newLine ? 1 : 0, Expression: value, Device: part.Id);
    }

    private SketchInstruction Cursor(Component part, bool oled)
    {
        var column = number(next(), 0, oled ? OledWidth - 1 : LcdColumns - 1);
        expect(",");
        var row = number(next(), 0, oled ? OledHeight - 1 : LcdRows - 1);
        var index = oled
            ? row / OledCellHeight * OledColumns + Math.Min(OledColumns - 1, column / OledCellWidth)
            : row * LcdColumns + column;
        return new(SketchOperation.DisplayCursor, Device: part.Id, Value: index);
    }

    private SketchInstruction? OledTextSize()
    {
        number(next(), 1, 1);
        return null;
    }

    private SketchInstruction? OledTextColor()
    {
        var color = next();
        if (color.Text is not ("SSD1306_WHITE" or "WHITE" or "1"))
            throw new SketchSyntaxException(color, "Для текста OLED используйте SSD1306_WHITE.");
        return null;
    }

    private (string Sda, string Scl) BusPins() => board.Kind switch
    {
        BoardKind.Esp32 => ("GPIO21", "GPIO22"),
        BoardKind.Nano => ("A4", "A5"),
        _ => ("SDA", "SCL")
    };

    private Component FindI2c(CppToken token, string kind, string title)
    {
        var address = number(token, ComponentProperties.MinimumI2cAddress, ComponentProperties.MaximumI2cAddress);
        var hex = $"0x{address:X2}";
        var (sda, scl) = BusPins();
        var bus = circuit.Project.Circuit.Components.Where(part => part.I2cAddress is not null &&
            Connected(sda, part, "SDA") && Connected(scl, part, "SCL")).ToArray();
        var matches = bus.Where(part => string.Equals(part.I2cAddress, hex, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length > 1)
            throw new SketchSyntaxException(token, $"Адрес {hex} уже общий для {string.Join(", ", matches.Select(part => part.Reference))}. " +
                "Укажите разные I²C-адреса в свойствах дисплеев и те же адреса в скетче.");
        if (matches.Length == 1 && matches[0].Definition.Id == kind) return matches[0];
        var connected = string.Join(", ", bus.Where(part => part.Definition.Id == kind)
            .Select(part => $"{part.Reference} — {part.I2cAddress}"));
        throw new SketchSyntaxException(token, $"{title} с адресом {hex} не найден на линиях {sda} и {scl}. " +
            (connected.Length == 0 ? "Проверьте подключение SDA и SCL." : $"Адреса подключённых {title}: {connected}."));
    }

    private void EnsureDistinct(CppToken name, Component part)
    {
        var previous = displays.FirstOrDefault(pair => pair.Key != name.Text && pair.Value.Part?.Id == part.Id).Key;
        if (previous is not null)
            throw new SketchSyntaxException(name, $"«{previous}» и «{name.Text}» обращаются к одному " +
                $"{(part.Definition.Id == "oled-ssd1306" ? "OLED" : "LCD")} {part.Reference}" +
                (part.I2cAddress is { } address ? $" по адресу {address}. Для второго дисплея укажите его адрес." :
                    ". Для второго дисплея укажите его пины."));
    }

    private bool Connected(string boardPin, Component part, string devicePin) =>
        circuit.Nets.Find(controller, boardPin)?.Id is { } net && circuit.Nets.Find(part.Id, devicePin)?.Id == net;
}
