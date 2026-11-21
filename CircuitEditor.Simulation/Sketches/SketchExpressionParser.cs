using CircuitEditor.Core.Code;

namespace CircuitEditor.Simulation.Sketches;

internal sealed class SketchExpressionParser(Func<string, bool> take, Action<string> expect,
    Func<CppToken> next, Func<CppToken, BoardPinDefinition> pin, SketchSymbols symbols)
{
    private const int MaximumDepth = 16;

    public SketchExpression Parse() => Choice(0);

    private SketchExpression Choice(int depth)
    {
        var condition = Comparison(depth);
        if (!take("?")) return condition;
        var yes = Choice(depth + 1);
        expect(":");
        return new(SketchExpressionKind.Choose, Left: condition, Right: yes, Otherwise: Choice(depth + 1));
    }

    private SketchExpression Comparison(int depth)
    {
        var left = Difference(depth);
        if (take("="))
        {
            expect("=");
            return new(SketchExpressionKind.Equal, Left: left, Right: Difference(depth));
        }
        if (take("!"))
        {
            expect("=");
            return new(SketchExpressionKind.NotEqual, Left: left, Right: Difference(depth));
        }
        if (take(">"))
        {
            expect("=");
            return new(SketchExpressionKind.AtLeast, Left: left, Right: Difference(depth));
        }
        return left;
    }

    private SketchExpression Difference(int depth)
    {
        var left = Atom(depth);
        return take("-") ? new(SketchExpressionKind.Subtract, Left: left, Right: Atom(depth)) : left;
    }

    private SketchExpression Atom(int depth)
    {
        if (depth >= MaximumDepth) throw new SketchSyntaxException(next(), "Слишком длинное выражение.");
        if (take("!")) return new(SketchExpressionKind.Not, Left: Atom(depth + 1));
        if (take("("))
        {
            var expression = Choice(depth + 1);
            expect(")");
            return expression;
        }
        var token = next();
        if (token.Text == "millis")
        {
            expect("(");
            expect(")");
            return new(SketchExpressionKind.Millis);
        }
        if (token.Text == "digitalRead")
        {
            expect("(");
            var gpio = pin(next());
            expect(")");
            return new(SketchExpressionKind.DigitalRead, Pin: gpio.Id);
        }
        if (symbols.Slot(token) is int slot) return new(SketchExpressionKind.Variable, slot);
        if (SketchSymbols.Scalar(symbols.Resolve(token, constantUse: true)) is long value)
            return new(SketchExpressionKind.Constant, value);
        throw new SketchSyntaxException(token, $"«{token.Text}» нельзя вычислить. Используйте число, переменную, millis() или digitalRead().");
    }
}
