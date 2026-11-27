using CircuitEditor.Simulation.Gpio;

namespace CircuitEditor.Simulation.Components;

internal sealed class ComponentMemory
{
    public double Angle { get; set; }
    public double TargetAngle { get; set; }
    public long AngleUpdatedAt { get; set; }
    public double CollectorLimit { get; set; }
    public bool AlternateCollector { get; set; }
    public bool Powered { get; set; }
    public bool Echo { get; set; }
    public long EchoStartedAt { get; set; }
    public long EchoEndsAt { get; set; }
    public long? TriggerStart { get; set; }
    public DigitalLevel Level { get; set; } = DigitalLevel.Z;
    public string? Text { get; set; }
    public string? DisplayBuffer { get; set; }
    public int DisplayCursor { get; set; }
}
