namespace CircuitEditor.Core.Wiring;
/// <summary>Допустимые цвета изолированных перемычек.</summary>
public static class WirePalette
{
    /// <summary>Красный, чёрный, синий, зелёный, жёлтый, белый и оранжевый.</summary>
    public static IReadOnlyList<string> Colors { get; } = Array.AsReadOnly(new[] { "#FF3B5C", "#202530", "#307EFF", "#39FF88", "#FFD633", "#FFFFFF", "#FF952B" });
}
