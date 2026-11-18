using CircuitEditor.Core.Models;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;

namespace CircuitEditor.Core.Services;

/// <summary>Проверка и применение совмещения физических контактов и отверстий.</summary>
public interface IComponentInsertionService
{
    /// <summary>Проверяет предложение без изменения проекта.</summary>
    InsertionResult Preview(CircuitProject project, Guid componentId, FootprintDefinition footprint,
        Guid boardId, ComponentPlacement proposed);
    /// <summary>Применяет только полностью допустимую вставку.</summary>
    InsertionResult Insert(CircuitProject project, Guid componentId, FootprintDefinition footprint,
        Guid boardId, ComponentPlacement proposed);
}
