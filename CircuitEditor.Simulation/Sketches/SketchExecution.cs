using System.Globalization;
using CircuitEditor.Core.Inputs;
using CircuitEditor.Simulation.Engine;
using CircuitEditor.Simulation.Gpio;

namespace CircuitEditor.Simulation.Sketches;

internal sealed class SketchExecution(SketchProgram program, SimulationSession session)
{
    private const int InstructionsPerSlice = 256;
    private const int MicrosecondsPerMillisecond = 1_000;
    private readonly long[] variables = program.Variables.ToArray();
    private readonly Dictionary<string, string> servoPins = new(StringComparer.Ordinal);
    private bool inSetup = true;
    private int instruction;
    private long iterationStart;
    private int? analogResolution;

    public void Start() => session.ScheduleSketchStep(session.ModelTime, Run);

    private void Run()
    {
        var now = session.ModelTime;
        for (var count = 0; count < InstructionsPerSlice && !session.HasHalted; count++)
        {
            var code = inSetup ? program.Setup : program.Loop;
            if (instruction == code.Count)
            {
                instruction = 0;
                if (inSetup)
                {
                    inSetup = false;
                    iterationStart = now;
                    continue;
                }
                if (code.Count == 0) return;
                if (iterationStart == now)
                {
                    Schedule(SimulationEngine.StepMicroseconds);
                    return;
                }
                iterationStart = now;
            }

            var command = code[instruction++];
            switch (command.Operation)
            {
                case SketchOperation.Delay:
                    if (command.Value == 0) break;
                    Schedule(command.Value);
                    return;
                case SketchOperation.Branch:
                    if (ValueOf(command.Expression!) == 0) instruction = command.Target;
                    break;
                case SketchOperation.Assign:
                    if (command.Expression is { Kind: SketchExpressionKind.PulseIn } pulse)
                    {
                        WaitForEcho((int)command.Value, pulse);
                        return;
                    }
                    variables[(int)command.Value] = ValueOf(command.Expression!);
                    break;
                case SketchOperation.AnalogResolution:
                    analogResolution = (int)command.Value;
                    break;
                case SketchOperation.Jump:
                    instruction = command.Target;
                    break;
                case SketchOperation.Return:
                    instruction = code.Count;
                    break;
                case SketchOperation.DisplayText:
                    session.SketchDisplays.Write(command.Device, CommandText(command), command.Value != 0);
                    break;
                case SketchOperation.DisplayClear:
                    session.SketchDisplays.Clear(command.Device);
                    break;
                case SketchOperation.DisplayCursor:
                    session.SketchDisplays.Cursor(command.Device, (int)command.Value);
                    break;
                case SketchOperation.DisplayFlush:
                    session.SketchDisplays.Flush(command.Device);
                    break;
                case SketchOperation.ServoAttach:
                    DetachServo(command.Text);
                    servoPins[command.Text] = command.Pin;
                    session.SetSketchServo(new(program.Controller, command.Pin), 90);
                    break;
                case SketchOperation.ServoWrite:
                    if (servoPins.TryGetValue(command.Text, out var servoPin))
                        session.SetSketchServo(new(program.Controller, servoPin), ValueOf(command.Expression!));
                    break;
                case SketchOperation.ServoDetach:
                    DetachServo(command.Text);
                    break;
                case SketchOperation.SerialLine:
                    session.AppendSerial(CommandText(command));
                    break;
                case SketchOperation.SerialDht:
                    session.PrintSketchSensor(command.Device, (ComponentInput)command.Value);
                    break;
                default:
                    Apply(command);
                    break;
            }
        }
        if (!session.HasHalted) Schedule(SimulationEngine.StepMicroseconds);
    }

    private void Apply(SketchInstruction command)
    {
        var old = session.Circuit.Controllers[program.Controller].Get(command.Pin);
        var value = command.Expression is { } expression ? ValueOf(expression) : command.Value;
        var high = value != 0;
        var state = command.Operation switch
        {
            SketchOperation.PinMode => new((GpioMode)command.Value, old.High),
            SketchOperation.DigitalWrite when old.Role == CircuitEditor.Core.Models.PinRole.Input =>
                new(high ? GpioMode.InputPullup : GpioMode.Input, high),
            SketchOperation.AnalogWrite when value is > 0 and < SketchParser.PwmMaximum =>
                new GpioState(GpioMode.Pwm, Duty: value / (double)SketchParser.PwmMaximum),
            SketchOperation.AnalogWrite => new(GpioMode.Output, value >= SketchParser.PwmMaximum),
            _ => new(GpioMode.Output, high)
        };
        session.SetPin(new(program.Controller, command.Pin), state);
    }

    private string CommandText(SketchInstruction command) => command.Expression is { } numeric
        ? ValueOf(numeric).ToString(CultureInfo.InvariantCulture)
        : command.Text;

    private void DetachServo(string name)
    {
        if (servoPins.Remove(name, out var pin)) session.SetPin(new(program.Controller, pin), new(GpioMode.Input));
    }

    private void WaitForEcho(int target, SketchExpression expression)
    {
        const long maximumTimeoutUs = 60_000_000;
        var timeout = Math.Clamp(ValueOf(expression.Left!), 0, maximumTimeoutUs);
        var width = session.ReadSketchEcho(new(program.Controller, expression.Pin));
        var valid = width > 0 && width <= timeout;
        session.ScheduleSketchStep(checked(session.ModelTime + (valid ? width : timeout)), () =>
        {
            variables[target] = valid ? width : 0;
            Run();
        });
    }

    private long ValueOf(SketchExpression expression) => expression.Kind switch
    {
        SketchExpressionKind.Constant => expression.Value,
        SketchExpressionKind.Variable => variables[(int)expression.Value],
        SketchExpressionKind.Millis => session.ModelTime / MicrosecondsPerMillisecond,
        SketchExpressionKind.DigitalRead => session.ReadSketchPin(new(program.Controller, expression.Pin)) == DigitalLevel.High ? 1 : 0,
        SketchExpressionKind.AnalogRead => session.ReadSketchAnalog(new(program.Controller, expression.Pin),
            expression.Value != 0, analogResolution),
        SketchExpressionKind.Negate => -ValueOf(expression.Left!),
        SketchExpressionKind.Not => ValueOf(expression.Left!) == 0 ? 1 : 0,
        SketchExpressionKind.Add => ValueOf(expression.Left!) + ValueOf(expression.Right!),
        SketchExpressionKind.Subtract => ValueOf(expression.Left!) - ValueOf(expression.Right!),
        SketchExpressionKind.Multiply => ValueOf(expression.Left!) * ValueOf(expression.Right!),
        SketchExpressionKind.Divide => Divide(ValueOf(expression.Left!), ValueOf(expression.Right!)),
        SketchExpressionKind.Modulo => Divide(ValueOf(expression.Left!), ValueOf(expression.Right!), remainder: true),
        SketchExpressionKind.Equal => ValueOf(expression.Left!) == ValueOf(expression.Right!) ? 1 : 0,
        SketchExpressionKind.NotEqual => ValueOf(expression.Left!) != ValueOf(expression.Right!) ? 1 : 0,
        SketchExpressionKind.AtLeast => ValueOf(expression.Left!) >= ValueOf(expression.Right!) ? 1 : 0,
        SketchExpressionKind.AtMost => ValueOf(expression.Left!) <= ValueOf(expression.Right!) ? 1 : 0,
        SketchExpressionKind.Less => ValueOf(expression.Left!) < ValueOf(expression.Right!) ? 1 : 0,
        SketchExpressionKind.Greater => ValueOf(expression.Left!) > ValueOf(expression.Right!) ? 1 : 0,
        SketchExpressionKind.Map => Map(expression.Arguments!),
        SketchExpressionKind.Choose => ValueOf(expression.Left!) != 0
            ? ValueOf(expression.Right!) : ValueOf(expression.Otherwise!),
        _ => throw new InvalidOperationException("Неизвестное выражение скетча.")
    };

    private long Map(IReadOnlyList<SketchExpression> arguments)
    {
        var value = ValueOf(arguments[0]);
        var fromLow = ValueOf(arguments[1]);
        var fromHigh = ValueOf(arguments[2]);
        var toLow = ValueOf(arguments[3]);
        var toHigh = ValueOf(arguments[4]);
        return Divide((value - fromLow) * (toHigh - toLow), fromHigh - fromLow) + toLow;
    }

    private long Divide(long left, long right, bool remainder = false)
    {
        if (right == 0 || left == long.MinValue && right == -1)
        {
            session.RejectSketch(new("Недопустимое деление в скетче.", Code: "CODE_RUN"));
            return 0;
        }
        return remainder ? left % right : left / right;
    }

    private void Schedule(long delay) => session.ScheduleSketchStep(checked(session.ModelTime + delay), Run);
}
