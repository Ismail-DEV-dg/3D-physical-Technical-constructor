namespace CircuitEditor.Simulation.Sketches;

internal enum SketchExpressionKind
{
    Constant, Variable, Millis, DigitalRead, AnalogRead, PulseIn, Negate, Not,
    Add, Subtract, Multiply, Divide, Modulo, Equal, NotEqual, AtLeast, AtMost, Less, Greater, Choose, Map
}
