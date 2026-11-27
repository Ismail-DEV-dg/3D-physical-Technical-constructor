using System.Text.Json;
using CircuitEditor.Core.Code;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Models;
using CircuitEditor.Simulation.Engine;

namespace CircuitEditor.Simulation.Sketches;

internal sealed class SketchPeripheralParser(SimulationCircuit circuit, Guid controller, CodeBoardProfile board,
    Func<CppToken> next, Func<CppToken> current, Func<string, bool> take, Action<string> expect,
    Func<CppToken> identifier, Func<CppToken, BoardPinDefinition> pin, Func<CppToken, long, long, long> number,
    Func<SketchExpression> expression)
{
    private readonly Dictionary<string, Component> sensors = new(StringComparer.Ordinal);
    private readonly HashSet<string> servos = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> servoPins = new(StringComparer.Ordinal);
    private readonly SketchDisplayParser displays = new(circuit, controller, board, next, current, take, expect, pin, number, expression);

    internal static bool IsSupportedHeader(string name) => name is "Arduino" or "Wire" or "LiquidCrystal_I2C" or
        "LiquidCrystal" or "Adafruit_GFX" or "Adafruit_SSD1306" or "Servo" or "ESP32Servo" or "DHT";

    internal bool IsDeclaration(string name) => name is "DHT" or "Servo" || SketchDisplayParser.Supports(name);

    internal void Declare(CppToken type)
    {
        var name = identifier();
        if (name.Text is "Serial" or "Wire" || sensors.ContainsKey(name.Text) || servos.Contains(name.Text) || displays.Contains(name.Text))
            throw new SketchSyntaxException(name, "Имя устройства уже используется.");
        if (type.Text == "Servo") servos.Add(name.Text);
        else if (type.Text == "DHT")
        {
            expect("(");
            sensors.Add(name.Text, FindDht());
            expect(")");
        }
        else displays.Declare(type.Text, name);
        expect(";");
    }

    private Component FindDht()
    {
        var data = pin(next());
        expect(",");
        var type = next();
        var id = type.Text switch { "DHT11" => "dht11", "DHT22" => "dht22", _ => "" };
        var matches = circuit.Project.Circuit.Components.Where(part => part.Definition.Id == id &&
            circuit.Nets.Find(controller, data.Id)?.Id is { } net && circuit.Nets.Find(part.Id, "DATA")?.Id == net).Take(2).ToArray();
        return matches.Length == 1 ? matches[0] : throw new SketchSyntaxException(type,
            $"Проверьте, что к {data.Id} подключён один датчик {type.Text}.");
    }

    internal SketchInstruction? ParseCall(CppToken receiver)
    {
        var method = next();
        expect("(");
        SketchInstruction? instruction;
        if (receiver.Text == "Serial") instruction = ParseSerial(method);
        else if (receiver.Text == "Wire") instruction = ParseWire(method);
        else if (servos.Contains(receiver.Text)) instruction = ParseServo(receiver, method);
        else if (displays.Contains(receiver.Text)) instruction = displays.ParseCall(receiver, method);
        else if (sensors.ContainsKey(receiver.Text) && method.Text == "begin") instruction = null;
        else throw new SketchSyntaxException(receiver, $"Устройство или метод «{receiver.Text}.{method.Text}» не поддерживается.");
        expect(")");
        return instruction;
    }

    internal SketchExpression ParseExpression(CppToken receiver)
    {
        var method = next();
        expect("(");
        var result = displays.ParseExpression(receiver, method);
        expect(")");
        return result;
    }

    private SketchInstruction? ParseWire(CppToken method)
    {
        if (method.Text != "begin") throw Unsupported(method);
        if (current().Text == ")") return null;
        var sda = next();
        var data = pin(sda);
        expect(",");
        var scl = next();
        var clock = pin(scl);
        if (data.Number != board.DefaultSdaNumber || clock.Number != board.DefaultSclNumber)
            throw new SketchSyntaxException(sda, $"В учебном запуске используйте штатные линии SDA/SCL платы {board.Kind}.");
        return null;
    }

    private SketchInstruction ParseServo(CppToken receiver, CppToken method)
    {
        if (method.Text == "write")
            return new(SketchOperation.ServoWrite, Expression: expression(), Text: receiver.Text);
        if (method.Text == "detach")
            return new(SketchOperation.ServoDetach, Text: receiver.Text);
        if (method.Text != "attach") throw Unsupported(method);
        var token = next();
        var output = pin(token);
        if (output.IsInputOnly) throw new SketchSyntaxException(token, $"Пин {output.Id} работает только на вход.");
        if (servoPins.TryGetValue(output.Id, out var previous) && previous != receiver.Text)
            throw new SketchSyntaxException(token, $"Сервоприводы {previous} и {receiver.Text} используют общий сигнал {output.Id}. " +
                "Для независимого управления выберите разные пины.");
        servoPins[output.Id] = receiver.Text;
        return new(SketchOperation.ServoAttach, Pin: output.Id, Text: receiver.Text);
    }

    private SketchInstruction? ParseSerial(CppToken method)
    {
        if (method.Text == "begin") { number(next(), 1, int.MaxValue); return null; }
        if (method.Text != "println") throw Unsupported(method);
        var argument = current();
        if (argument.Text.StartsWith('"'))
        {
            next();
            try { return new(SketchOperation.SerialLine, Text: JsonSerializer.Deserialize<string>(argument.Text) ?? ""); }
            catch (JsonException) { throw new SketchSyntaxException(argument, "Укажите текст в двойных кавычках."); }
        }
        if (!sensors.TryGetValue(argument.Text, out var sensor))
            return new(SketchOperation.SerialLine, Expression: expression());
        next();
        expect(".");
        var call = next();
        var input = call.Text switch
        {
            "readTemperature" => ComponentInput.Temperature,
            "readHumidity" => ComponentInput.Humidity,
            _ => throw Unsupported(call)
        };
        expect("(");
        expect(")");
        return new(SketchOperation.SerialDht, Value: (long)input, Device: sensor.Id);
    }

    private static SketchSyntaxException Unsupported(CppToken token) =>
        new(token, $"Команда «{token.Text}» недоступна в учебном запуске.");
}
