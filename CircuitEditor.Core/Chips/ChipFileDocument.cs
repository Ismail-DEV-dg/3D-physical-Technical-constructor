namespace CircuitEditor.Core.Chips;

/// <summary>Независимый файловый контракт одного переносимого определения.</summary>
public sealed record ChipFileDocument(int SchemaVersion, ChipDefinition Definition)
{
    /// <summary>Текущая версия контейнера .chip.</summary>
    public const int CurrentSchemaVersion = 1;
}
