using System.Globalization;
using System.Text.Json;
using CircuitEditor.Core.Code;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Core.Models;
using CircuitEditor.Simulation.Engine;

namespace CircuitEditor.Simulation.Sketches;

internal sealed class SketchPeripheralParser(SimulationCircuit circuit, Guid controller, BoardKind board,
    Func<CppToken> next, Action<string> expect, Func<CppToken> identifier, Func<CppToken, BoardPinDefinition> pin,
    Func<CppToken, long, long, long> number)
{
    private const int DisplayColumns = 16;
    private const int DisplayRows = 2;
    private const int MaximumI2cAddress = 0x7f;
    private readonly Dictionary<string, Component> devices = new(StringComparer.Ordinal);

    internal static bool IsSupportedHeader(string name) => name is "Arduino" or "Wire" or "LiquidCrystal_I2C" or "DHT";

    internal bool IsDeclaration(string name) => name is "LiquidCrystal_I2C" or "DHT";

    internal void Declare(CppToken type)
    {
        var name = identifier();
        if (name.Text == "Serial" || devices.ContainsKey(name.Text))
            throw new SketchSyntaxException(name, "Имя устройства уже используется.");
        expect("(");
        var device = type.Text == "DHT" ? FindDht() : FindDisplay();
        expect(")");
        expect(";");
        devices.Add(name.Text, device);
    }

    private Component FindDht()
    {
        var data = pin(next());
        expect(",");
        var type = next();
        var id = type.Text switch { "DHT11" => "dht11", "DHT22" => "dht22", _ => "" };
        return FindDevice(type, part => part.Definition.Id == id && Connected(data.Id, part, "DATA"),
            $"Проверьте, что к {data.Id} подключён один датчик {type.Text}.");
    }

    private Component FindDisplay()
    {
        var value = next();
        var address = number(value, 0, MaximumI2cAddress);
        expect(",");
        number(next(), DisplayColumns, DisplayColumns);
        expect(",");
        number(next(), DisplayRows, DisplayRows);
        var (sda, scl) = board switch
        {
            BoardKind.Esp32 => ("GPIO21", "GPIO22"),
            BoardKind.Nano => ("A4", "A5"),
            _ => ("SDA", "SCL")
        };
        var hex = address.ToString("X2", CultureInfo.InvariantCulture);
        return FindDevice(value, part => part.Definition.Id == "lcd1602-i2c" &&
            string.Equals(part.I2cAddress, "0x" + hex, StringComparison.OrdinalIgnoreCase) &&
            Connected(sda, part, "SDA") && Connected(scl, part, "SCL"),
            $"Проверьте адрес 0x{hex} у LCD и подключение линий {sda} и {scl}.");
    }

    internal SketchInstruction? ParseCall(CppToken receiver)
    {
        var method = next();
        expect("(");
        SketchInstruction? instruction;
        if (receiver.Text == "Serial") instruction = ParseSerial(method);
        else if (devices.TryGetValue(receiver.Text, out var device))
            instruction = ParseDevice(device, method);
        else throw new SketchSyntaxException(receiver, $"Устройство «{receiver.Text}» не объявлено.");
        expect(")");
        return instruction;
    }

    private SketchInstruction? ParseSerial(CppToken method)
    {
        if (method.Text == "begin") { number(next(), 1, int.MaxValue); return null; }
        if (method.Text != "println") throw Unsupported(method);
        var argument = next();
        if (argument.Text.StartsWith('"')) return new(SketchOperation.SerialLine, Text: ReadString(argument));
        if (!devices.TryGetValue(argument.Text, out var sensor) || sensor.Definition.Id is not ("dht11" or "dht22"))
            throw new SketchSyntaxException(argument, "Укажите текст в кавычках или показание DHT.");
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

    private SketchInstruction? ParseDevice(Component device, CppToken method)
    {
        if (device.Definition.Id != "lcd1602-i2c")
            return method.Text == "begin" ? null : throw Unsupported(method);
        return method.Text switch
        {
            "init" or "backlight" => null,
            "print" => new(SketchOperation.DisplayText, Device: device.Id, Text: ReadString(next())),
            "clear" => new(SketchOperation.DisplayClear, Device: device.Id),
            _ => throw Unsupported(method)
        };
    }

    private bool Connected(string boardPin, Component part, string devicePin) =>
        circuit.Nets.Find(controller, boardPin)?.Id is { } net && circuit.Nets.Find(part.Id, devicePin)?.Id == net;

    private Component FindDevice(CppToken token, Func<Component, bool> predicate, string error)
    {
        var matches = circuit.Project.Circuit.Components.Where(predicate).Take(2).ToArray();
        return matches.Length == 1 ? matches[0] : throw new SketchSyntaxException(token, error);
    }

    private static string ReadString(CppToken token)
    {
        try { return JsonSerializer.Deserialize<string>(token.Text) ?? throw Unsupported(token); }
        catch (JsonException) { throw new SketchSyntaxException(token, "Укажите текст в двойных кавычках."); }
    }

    private static SketchSyntaxException Unsupported(CppToken token) =>
        new(token, $"Команда «{token.Text}» недоступна в учебном запуске.");
}
