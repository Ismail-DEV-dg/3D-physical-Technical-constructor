namespace CircuitEditor.Core.Code;
/// <summary>Сохраняемые данные скетча; не участвуют в истории электрической схемы.</summary>
public sealed record SketchDocument(string Text = "", BoardKind Board = BoardKind.Uno, string Name = "Sketch",
    Guid? ControllerComponentId = null, string? GeneratedCircuitSignature = null)
{
    /// <summary>Максимальный размер текста для защиты загрузки и редактора.</summary>
    public const int MaximumTextLength = 2_000_000;
    /// <summary>Проверяет поля, не обращаясь к файловой системе.</summary>
    public bool IsValid => Text is not null && Text.Length <= MaximumTextLength && Enum.IsDefined(Board)
        && !string.IsNullOrWhiteSpace(Name) && ControllerComponentId != Guid.Empty;
}
