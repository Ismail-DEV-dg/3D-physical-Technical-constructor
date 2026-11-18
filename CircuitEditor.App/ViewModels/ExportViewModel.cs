using CircuitEditor.App.Services;
using CircuitEditor.Core.Export;
using CommunityToolkit.Mvvm.Input;
namespace CircuitEditor.App.ViewModels;
public sealed partial class ExportViewModel(ProjectViewModel project, INavigationService navigation, IDialogService dialogs,
    IImageExportService images, ISvgExportService svg, IBomExportService bom)
{
    [RelayCommand]
    private void Png()
    {
        if (navigation.Current.Kind is not (WorkspaceKind.Mockup or WorkspaceKind.Schematic)) { project.StatusMessage = "Для PNG выберите Макет или Схему."; return; }
        var scale = dialogs.ChooseExportScale(); if (scale is null) return;
        var file = dialogs.SaveFile("PNG (*.png)|*.png",".png"); if (file is null) return;
        project.StatusMessage = images.ExportPng(file,navigation.Current.Kind,scale.Value) ?? "PNG экспортирован.";
    }
    [RelayCommand]
    private void Svg()
    {
        if (navigation.Current.Kind != WorkspaceKind.Schematic) { project.StatusMessage = "SVG доступен только для Схемы."; return; }
        var file = dialogs.SaveFile("SVG (*.svg)|*.svg",".svg"); if (file is null) return;
        project.StatusMessage = svg.ExportSchematic(file) ?? "SVG схемы экспортирован.";
    }
    [RelayCommand]
    private void Bom()
    {
        var file = dialogs.SaveFile("CSV (*.csv)|*.csv",".csv"); if (file is null) return;
        project.StatusMessage = bom.Export(file,project.Model) ?? "BOM экспортирован в CSV.";
    }
}
