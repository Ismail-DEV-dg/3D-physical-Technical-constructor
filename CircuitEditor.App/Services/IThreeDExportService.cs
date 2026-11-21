namespace CircuitEditor.App.Services;

public interface IThreeDExportService
{
    string? ExportPng(string path, double scale);
    string? ExportObj(string path);
}
