using CircuitEditor.App.ViewModels;
namespace CircuitEditor.App.Services;
public interface IImageExportService
{
    string? ExportPng(string path, WorkspaceKind kind, double scale);
}
