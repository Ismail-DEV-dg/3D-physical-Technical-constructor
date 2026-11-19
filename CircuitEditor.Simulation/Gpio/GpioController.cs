using CircuitEditor.Core.Models;
using CircuitEditor.Core.Simulation;

namespace CircuitEditor.Simulation.Gpio;

public sealed class GpioController(Component board, SimulationBoardProfile profile)
{
    private readonly Dictionary<string, GpioState> states = new(StringComparer.Ordinal);
    public Guid Id => board.Id;
    public string Reference => board.Reference;
    public SimulationBoardProfile Profile => profile;
    public IReadOnlyList<Pin> Pins { get; } = board.Definition.Pins.Where(IsGpio).ToArray();
    public GpioState Get(string pinId) => states.GetValueOrDefault(pinId) ?? new();

    public bool TrySet(string pinId, GpioState state)
    {
        var pin = Pins.FirstOrDefault(p => p.Id == pinId);
        if (pin is null || !Enum.IsDefined(state.Mode) || !double.IsFinite(state.Duty) || state.Duty is < 0 or > 1
            || state.AnalogOverride is double analog && (!double.IsFinite(analog) || analog is < 0 or > 1)
            || state.PeriodMicroseconds is < 1 or > 1_000_000
            || state.Mode != GpioMode.Input && pin.IsInputOnly
            || state.Mode == GpioMode.Pwm && pin.Type != PinType.PWM && !pin.Capabilities.Contains(PinType.PWM))
            return false;
        states[pinId] = state;
        return true;
    }

    public double? Drive(string id, double pwmPhase)
    {
        var state = Get(id);
        return state.Mode switch
        {
            GpioMode.Output => state.High ? Profile.LogicVoltage : 0,
            GpioMode.InputPullup => Profile.LogicVoltage,
            GpioMode.Pwm => pwmPhase < state.Duty ? Profile.LogicVoltage : 0,
            _ => null
        };
    }

    public DigitalLevel OutputAt(string id, long microseconds)
    {
        var state = Get(id);
        if (state.Role != PinRole.Output) return DigitalLevel.Z;
        var period = Period(id);
        return Drive(id, (double)(microseconds % period) / period) > 0
            ? DigitalLevel.High : DigitalLevel.Low;
    }

    public long Period(string id) => Get(id).PeriodMicroseconds ?? Profile.PwmPeriodMicroseconds;

    public int? ReadAdc(string id, double? voltage)
    {
        var pin = Pins.FirstOrDefault(p => p.Id == id);
        if (pin is null || pin.Type != PinType.AnalogIn && !pin.Capabilities.Contains(PinType.AnalogIn)) return null;
        var fraction = Get(id).AnalogOverride ?? voltage / Profile.LogicVoltage;
        return fraction is double value ? (int)Math.Round(Math.Clamp(value, 0, 1) * ((1 << Profile.AdcBits) - 1)) : null;
    }

    public static bool IsGpio(Pin pin) => pin.IsExposed && !pin.IsNoConnect &&
        (pin.Id.StartsWith("GPIO", StringComparison.Ordinal)
            || pin.Id.Length > 1 && pin.Id[0] is 'D' or 'A' && pin.Id.AsSpan(1).ToString().All(char.IsAsciiDigit));
}
