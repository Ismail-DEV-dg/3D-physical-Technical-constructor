using CircuitEditor.Core.Models;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Physical;
using CircuitEditor.Core.Placement;

namespace CircuitEditor.Core.Services;

/// <summary>Проверка и применение совмещения физических контактов и отверстий.</summary>
public interface IComponentInsertionService
{
    /// <summary>Проверяет предложение без изменения проекта.</summary>
    InsertionResult Preview(CircuitProject project, Guid componentId, FootprintDefinition footprint,
        Guid boardId, ComponentPlacement proposed);
    /// <summary>Подбирает допустимую посадку детали, ещё не добавленной в проект.</summary>
    InsertionResult PreviewNew(CircuitProject project, ComponentDefinition definition, FootprintDefinition footprint,
        Guid boardId, LayoutPoint pointer, int? rotation = null);
    /// <summary>Применяет только полностью допустимую вставку.</summary>
    InsertionResult Insert(CircuitProject project, Guid componentId, FootprintDefinition footprint,
        Guid boardId, ComponentPlacement proposed);
}
