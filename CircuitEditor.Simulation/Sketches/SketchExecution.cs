using CircuitEditor.Simulation.Engine;
using CircuitEditor.Simulation.Gpio;

namespace CircuitEditor.Simulation.Sketches;

internal sealed class SketchExecution(SketchProgram program, SimulationSession session)
{
    private const int InstructionsPerSlice = 256;
    private const int MicrosecondsPerMillisecond = 1_000;
    private readonly long[] variables = program.Variables.ToArray();
    private bool inSetup = true;
    private int instruction;
    private long iterationStart;

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
                    variables[(int)command.Value] = ValueOf(command.Expression!);
                    break;
                case SketchOperation.Jump:
                    instruction = command.Target;
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
        var high = (command.Expression is { } expression ? ValueOf(expression) : command.Value) != 0;
        var state = command.Operation switch
        {
            SketchOperation.PinMode => new((GpioMode)command.Value, old.High),
            SketchOperation.DigitalWrite when old.Role == CircuitEditor.Core.Models.PinRole.Input =>
                new(high ? GpioMode.InputPullup : GpioMode.Input, high),
            SketchOperation.AnalogWrite when command.Value is > 0 and < SketchParser.PwmMaximum =>
                new GpioState(GpioMode.Pwm, Duty: command.Value / (double)SketchParser.PwmMaximum),
            _ => new(GpioMode.Output, high)
        };
        session.SetPin(new(program.Controller, command.Pin), state);
    }

    private long ValueOf(SketchExpression expression) => expression.Kind switch
    {
        SketchExpressionKind.Constant => expression.Value,
        SketchExpressionKind.Variable => variables[(int)expression.Value],
        SketchExpressionKind.Millis => session.ModelTime / MicrosecondsPerMillisecond,
        SketchExpressionKind.DigitalRead => session.ReadSketchPin(new(program.Controller, expression.Pin)) == DigitalLevel.High ? 1 : 0,
        SketchExpressionKind.Not => ValueOf(expression.Left!) == 0 ? 1 : 0,
        SketchExpressionKind.Subtract => ValueOf(expression.Left!) - ValueOf(expression.Right!),
        SketchExpressionKind.Equal => ValueOf(expression.Left!) == ValueOf(expression.Right!) ? 1 : 0,
        SketchExpressionKind.NotEqual => ValueOf(expression.Left!) != ValueOf(expression.Right!) ? 1 : 0,
        SketchExpressionKind.AtLeast => ValueOf(expression.Left!) >= ValueOf(expression.Right!) ? 1 : 0,
        SketchExpressionKind.Choose => ValueOf(expression.Left!) != 0
            ? ValueOf(expression.Right!) : ValueOf(expression.Otherwise!),
        _ => throw new InvalidOperationException("Неизвестное выражение скетча.")
    };

    private void Schedule(long delay) => session.ScheduleSketchStep(checked(session.ModelTime + delay), Run);
}
