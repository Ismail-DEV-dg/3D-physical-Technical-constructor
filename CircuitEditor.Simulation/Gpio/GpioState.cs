using CircuitEditor.Core.Models;

namespace CircuitEditor.Simulation.Gpio;

public sealed record GpioState(GpioMode Mode = GpioMode.Input, bool High = false, double Duty = 0, double? AnalogOverride = null, long? PeriodMicroseconds = null)
{
    public PinRole Role => Mode is GpioMode.Output or GpioMode.Pwm ? PinRole.Output : PinRole.Input;

    internal double DriveSetting() => Mode == GpioMode.Pwm ? Duty : Mode == GpioMode.Output && High ? 1 : 0;
}
