namespace CircuitEditor.Core.Code;
/// <summary>Сохраняемые данные скетча; не участвуют в истории электрической схемы.</summary>
public sealed record SketchDocument(string Text = SketchDocument.StarterText, BoardKind Board = BoardKind.Uno, string Name = "Sketch",
    Guid? ControllerComponentId = null, string? GeneratedCircuitSignature = null)
{
    /// <summary>Пустой исполняемый скетч Arduino.</summary>
    public const string StarterText = "// -Abd IL-\n\nvoid setup() {\n\n}\n\nvoid loop() {\n\n}\n";

    /// <summary>Дополняет открываемый или запускаемый черновик, сохраняя написанный код.</summary>
    public SketchDocument WithEntryPoints()
    {
        var text = SketchEntryPoints.Ensure(Text);
        return text == Text ? this : this with { Text = text };
    }

    /// <summary>Максимальный размер текста для защиты загрузки и редактора.</summary>
    public const int MaximumTextLength = 2_000_000;
    /// <summary>Проверяет поля, не обращаясь к файловой системе.</summary>
    public bool IsValid => Text is not null && Text.Length <= MaximumTextLength && Enum.IsDefined(Board)
        && !string.IsNullOrWhiteSpace(Name) && ControllerComponentId != Guid.Empty;
}
