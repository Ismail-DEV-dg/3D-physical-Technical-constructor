using CircuitEditor.Core.Code;

namespace CircuitEditor.Simulation.Sketches;

internal sealed class SketchExpressionParser(Func<string, bool> take, Action<string> expect,
    Func<CppToken> next, Func<CppToken, BoardPinDefinition> pin, SketchSymbols symbols,
    Func<CppToken, SketchExpression> peripheral)
{
    private const int MaximumDepth = 16;
    private const long DefaultPulseTimeoutUs = 1_000_000;

    public SketchExpression Parse() => Choice(0);

    public SketchExpression ParseAssignment()
    {
        if (!take("pulseIn")) return Parse();
        expect("(");
        var gpio = pin(next());
        expect(",");
        var level = next();
        if (symbols.Resolve(level, constantUse: true) is not ("HIGH" or "1"))
            throw new SketchSyntaxException(level, "Для измерения импульса ECHO укажите HIGH.");
        var timeout = take(",") ? Parse() : new(SketchExpressionKind.Constant, DefaultPulseTimeoutUs);
        expect(")");
        return new(SketchExpressionKind.PulseIn, Pin: gpio.Id, Left: timeout);
    }

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
        var left = Sum(depth);
        if (take("="))
        {
            expect("=");
            return new(SketchExpressionKind.Equal, Left: left, Right: Sum(depth));
        }
        if (take("!"))
        {
            expect("=");
            return new(SketchExpressionKind.NotEqual, Left: left, Right: Sum(depth));
        }
        if (take(">"))
        {
            var kind = take("=") ? SketchExpressionKind.AtLeast : SketchExpressionKind.Greater;
            return new(kind, Left: left, Right: Sum(depth));
        }
        if (take("<"))
        {
            var kind = take("=") ? SketchExpressionKind.AtMost : SketchExpressionKind.Less;
            return new(kind, Left: left, Right: Sum(depth));
        }
        return left;
    }

    private SketchExpression Sum(int depth)
    {
        SketchExpressionKind? Operator() => take("+") ? SketchExpressionKind.Add :
            take("-") ? SketchExpressionKind.Subtract : null;
        var left = Product(depth);
        for (var count = 0; Operator() is { } kind; count++)
        {
            CheckChain(count);
            left = new(kind, Left: left, Right: Product(depth));
        }
        return left;
    }

    private SketchExpression Product(int depth)
    {
        SketchExpressionKind? Operator() => take("*") ? SketchExpressionKind.Multiply :
            take("/") ? SketchExpressionKind.Divide : take("%") ? SketchExpressionKind.Modulo : null;
        var left = Atom(depth);
        for (var count = 0; Operator() is { } kind; count++)
        {
            CheckChain(count);
            left = new(kind, Left: left, Right: Atom(depth));
        }
        return left;
    }

    private void CheckChain(int count)
    {
        if (count >= MaximumDepth) throw new SketchSyntaxException(next(), "Слишком длинное выражение.");
    }

    private SketchExpression Atom(int depth)
    {
        if (depth >= MaximumDepth) throw new SketchSyntaxException(next(), "Слишком длинное выражение.");
        if (take("!")) return new(SketchExpressionKind.Not, Left: Atom(depth + 1));
        if (take("-")) return new(SketchExpressionKind.Negate, Left: Atom(depth + 1));
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
        if (token.Text is "digitalRead" or "analogRead" or "analogReadMilliVolts")
        {
            expect("(");
            var argument = next();
            var gpio = pin(argument);
            expect(")");
            if (token.Text == "digitalRead") return new(SketchExpressionKind.DigitalRead, Pin: gpio.Id);
            if (!gpio.IsAnalog) throw new SketchSyntaxException(argument, $"Пин {gpio.Id} не поддерживает аналоговый вход.");
            return new(SketchExpressionKind.AnalogRead, Value: token.Text == "analogReadMilliVolts" ? 1 : 0, Pin: gpio.Id);
        }
        if (token.Text == "map")
        {
            expect("(");
            var arguments = new SketchExpression[5];
            for (var i = 0; i < arguments.Length; i++)
            {
                if (i != 0) expect(",");
                arguments[i] = Choice(depth + 1);
            }
            expect(")");
            return new(SketchExpressionKind.Map, Arguments: arguments);
        }
        if (take(".")) return peripheral(token);
        if (symbols.Slot(token) is int slot) return new(SketchExpressionKind.Variable, slot);
        if (SketchSymbols.Scalar(symbols.Resolve(token, constantUse: true)) is long value)
            return new(SketchExpressionKind.Constant, value);
        throw new SketchSyntaxException(token, $"«{token.Text}» нельзя вычислить. Используйте число, переменную, millis() или digitalRead().");
    }
}
