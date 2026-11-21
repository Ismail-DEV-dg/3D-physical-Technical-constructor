using CircuitEditor.Core.Code;

namespace CircuitEditor.Simulation.Sketches;

internal sealed class SketchSyntaxException(CppToken token, string message) : Exception(message)
{
    public CodeProblem Problem => new(Message, token.Line, token.Column, "Error", "CODE_RUN");
}
