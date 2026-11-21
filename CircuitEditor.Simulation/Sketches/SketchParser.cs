using CircuitEditor.Core.Code;
using CircuitEditor.Simulation.Gpio;

namespace CircuitEditor.Simulation.Sketches;

internal sealed class SketchParser
{
    private const int MaximumSourceLength = 128 * 1024;
    private const int MaximumBlockDepth = 24;
    internal const int PwmMaximum = 255;
    private const long MicrosecondsPerMillisecond = 1000;
    private const long MaximumDelayMicroseconds = 3_600_000_000;
    private static readonly HashSet<string> Types = new(StringComparer.Ordinal)
        { "int", "long", "short", "byte", "bool", "uint8_t", "uint16_t", "uint32_t", "unsigned" };
    private static readonly IReadOnlyDictionary<string, string> NoAliases = new Dictionary<string, string>();
    private readonly SketchSymbols symbols = new();
    private readonly SketchExpressionParser expressions;
    private readonly IReadOnlyList<CppToken> tokens;
    private readonly CodeBoardProfile profile;
    private readonly CancellationToken cancellation;
    private readonly CppToken end;
    private int cursor;

    public SketchParser(string text, CodeBoardProfile profile, CancellationToken cancellation)
    {
        this.profile = profile;
        this.cancellation = cancellation;
        expressions = new(Take, Expect, Next, Pin, symbols);
        if (text.Length > MaximumSourceLength)
            throw new SketchSyntaxException(new("", 0, 1, 1), "Для учебного запуска скетч должен занимать не более 131 072 символов.");
        tokens = CppTokenScanner.Scan(text, includeLiterals: true);
        var last = tokens.LastOrDefault();
        end = last is null ? new("", text.Length, 1, 1) :
            new("", text.Length, last.Line, last.Column + last.Text.Length);
    }

    public SketchProgram Parse(Guid controller)
    {
        List<SketchInstruction>? setup = null;
        List<SketchInstruction>? loop = null;
        while (!AtEnd)
        {
            cancellation.ThrowIfCancellationRequested();
            if (Take("#")) { Directive(); continue; }
            if (!Take("void")) { Declare(); continue; }
            var function = Next();
            if (function.Text is not ("setup" or "loop") ||
                (function.Text == "setup" ? setup is not null : loop is not null))
                throw Error(function, "Нужны ровно по одной функции void setup() и void loop().");
            Expect("(");
            Take("void");
            Expect(")");
            Expect("{");
            List<SketchInstruction> body = [];
            Block(body, 1);
            if (function.Text == "setup") setup = body;
            else loop = body;
        }

        if (setup is null || loop is null) throw Error(end, "Добавьте обе функции: void setup() и void loop().");
        return new(controller, setup, loop, symbols.InitialValues);
    }

    private void Directive()
    {
        var directive = Next();
        var line = directive.Line;
        if (directive.Text == "define")
        {
            var name = Identifier();
            if (name.Line != line || AtEnd || Current.Line != line)
                throw Error(directive, "Объявите константу в виде #define ИМЯ значение.");
            var value = Next();
            if (!AtEnd && Current.Line == line)
                throw Error(value, "Поддерживаются одно число или одно имя в #define.");
            symbols.Declare(name, value, mutable: false);
            return;
        }
        if (directive.Text == "include")
        {
            if (Take("<"))
            {
                if (Current.Line == line && Take("Arduino") && Take(".") && Take("h") && Take(">") &&
                    (AtEnd || Current.Line != line)) return;
            }
            else if (Current.Line == line && Take("\"Arduino.h\"") && (AtEnd || Current.Line != line)) return;
        }
        throw Error(directive, "Для запуска поддерживаются #define и подключение Arduino.h. Внешние библиотеки требуют Arduino IDE.");
    }

    private void Declare(bool inside = false)
    {
        var staticStorage = Take("static");
        var constant = Take("const") || Take("constexpr");
        var type = Next();
        if (!Types.Contains(type.Text) || inside && !constant && !staticStorage)
            throw Error(type, "В блоке используйте const или static с целым или логическим значением.");
        if (type.Text == "unsigned" && Current.Text is "int" or "long" or "short") Next();
        var name = Identifier();
        Expect("=");
        var value = Next();
        Expect(";");
        symbols.Declare(name, value, mutable: !constant);
    }

    private void Block(List<SketchInstruction> body, int depth)
    {
        if (depth > MaximumBlockDepth) throw Error(Current, "Слишком много вложенных блоков.");
        symbols.Enter();
        while (!Take("}"))
        {
            if (AtEnd) throw Error(end, "Не найдена закрывающая скобка «}».");
            Statement(body, depth);
        }
        symbols.Leave();
    }

    private void Statement(List<SketchInstruction> body, int depth)
    {
        cancellation.ThrowIfCancellationRequested();
        if (depth > MaximumBlockDepth) throw Error(Current, "Слишком много вложенных блоков.");
        if (Take(";")) return;
        if (Take("{")) { Block(body, depth + 1); return; }
        if (Take("if")) { Conditional(body, depth); return; }

        if (Current.Text is "static" or "const" or "constexpr") { Declare(inside: true); return; }
        var command = Next();
        if (Take("="))
        {
            var slot = symbols.WritableSlot(command);
            var assigned = expressions.Parse();
            Expect(";");
            body.Add(new(SketchOperation.Assign, Value: slot, Expression: assigned));
            return;
        }
        if (command.Text is "delay" or "delayMicroseconds")
        {
            Expect("(");
            var unit = command.Text == "delay" ? MicrosecondsPerMillisecond : 1;
            var delay = Number(Next(), 0, MaximumDelayMicroseconds / unit);
            Expect(")");
            Expect(";");
            body.Add(new(SketchOperation.Delay, Value: delay * unit));
            return;
        }
        if (command.Text is not ("pinMode" or "digitalWrite" or "analogWrite"))
            throw Error(command, $"«{command.Text}» не поддерживается при запуске. Доступны pinMode, digitalWrite, analogWrite, delay и if с digitalRead.");

        Expect("(");
        var pinToken = Next();
        var pin = Pin(pinToken);
        Expect(",");
        var expression = command.Text == "digitalWrite" ? expressions.Parse() : null;
        var operation = command.Text switch
        {
            "pinMode" => SketchOperation.PinMode,
            "analogWrite" => SketchOperation.AnalogWrite,
            _ => SketchOperation.DigitalWrite
        };
        var value = operation switch
        {
            SketchOperation.PinMode => Mode(Next()),
            SketchOperation.AnalogWrite => Number(Next(), 0, PwmMaximum),
            _ => 0
        };
        if (pin.IsInputOnly && (operation != SketchOperation.PinMode || value != (long)GpioMode.Input))
            throw Error(pinToken, $"Пин {pin.Id} работает только на вход.");
        if (operation == SketchOperation.AnalogWrite && value is > 0 and < PwmMaximum && !pin.SupportsPwm)
            throw Error(pinToken, $"Пин {pin.Id} не поддерживает ШИМ.");
        Expect(")");
        Expect(";");
        body.Add(new(operation, pin.Id, value, Expression: expression));
    }

    private void Conditional(List<SketchInstruction> body, int depth)
    {
        Expect("(");
        var condition = expressions.Parse();
        Expect(")");
        var branch = body.Count;
        body.Add(new(SketchOperation.Branch, Expression: condition));
        Statement(body, depth + 1);
        if (Take("else"))
        {
            var jump = body.Count;
            body.Add(new(SketchOperation.Jump));
            body[branch] = body[branch] with { Target = body.Count };
            Statement(body, depth + 1);
            body[jump] = body[jump] with { Target = body.Count };
        }
        else body[branch] = body[branch] with { Target = body.Count };
    }

    private BoardPinDefinition Pin(CppToken token)
    {
        var value = symbols.Resolve(token, constantUse: true);
        return CodePinResolver.Resolve(value, profile, NoAliases) is { IsExposed: true } pin
            ? pin : throw Error(token, $"Пин «{token.Text}» отсутствует на плате {profile.Kind}.");
    }

    private long Mode(CppToken token) => symbols.Resolve(token, constantUse: true) switch
    {
        "INPUT" or "0" => (long)GpioMode.Input,
        "OUTPUT" or "1" => (long)GpioMode.Output,
        "INPUT_PULLUP" or "2" => (long)GpioMode.InputPullup,
        _ => throw Error(token, "Ожидается INPUT, OUTPUT или INPUT_PULLUP.")
    };

    private long Number(CppToken token, long min, long max)
    {
        var value = SketchSymbols.Scalar(symbols.Resolve(token, constantUse: true));
        return value is long number && number >= min && number <= max ? number :
            throw Error(token, $"Ожидается целое число от {min} до {max}.");
    }

    private CppToken Identifier()
    {
        var token = Next();
        if (!char.IsAsciiLetter(token.Text[0]) && token.Text[0] != '_') throw Error(token, "Ожидается имя константы.");
        return token;
    }

    private bool AtEnd => cursor >= tokens.Count;
    private CppToken Current => AtEnd ? end : tokens[cursor];
    private CppToken Next() => !AtEnd ? tokens[cursor++] : throw Error(end, "Неожиданный конец скетча.");
    private bool Take(string text)
    {
        if (Current.Text != text) return false;
        cursor++;
        return true;
    }
    private void Expect(string text)
    {
        if (!Take(text)) throw Error(Current, $"Ожидается «{text}».");
    }
    private static SketchSyntaxException Error(CppToken token, string message) => new(token, message);
}
