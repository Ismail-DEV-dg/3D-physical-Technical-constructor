namespace CircuitEditor.Simulation.Sketches;

internal enum SketchOperation
{
    PinMode, DigitalWrite, AnalogWrite, AnalogResolution, Delay, Branch, Jump, Assign, Return,
    DisplayText, DisplayClear, DisplayCursor, DisplayFlush, ServoAttach, ServoWrite, ServoDetach, SerialLine, SerialDht
}
