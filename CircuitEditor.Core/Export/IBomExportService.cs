using CircuitEditor.Core.Models;
namespace CircuitEditor.Core.Export;
/// <summary>Формирование и сохранение ведомости деталей.</summary>
public interface IBomExportService
{
    /// <summary>UTF-8 с BOM, разделитель точка с запятой.</summary>
    byte[] Create(CircuitProject project);
    /// <summary>Записывает ведомость через репозиторий.</summary>
    string? Export(string path, CircuitProject project);
}
