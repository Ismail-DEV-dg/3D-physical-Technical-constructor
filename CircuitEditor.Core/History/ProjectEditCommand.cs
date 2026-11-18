using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.History;
/// <summary>Обратимое атомарное действие над экземплярами, раскладками и привязками.</summary>
public sealed class ProjectEditCommand(string name, ProjectSnapshot before, ProjectSnapshot after, Action<CircuitProject> restore) : IEditCommand
{
    /// <inheritdoc/>
    public string Name => name;
    /// <inheritdoc/>
    public void Execute() => restore(after.Restore());
    /// <inheritdoc/>
    public void Undo() => restore(before.Restore());
}
