using System.Text;
using CircuitEditor.App.Services;
using CircuitEditor.Core.Chips;
using CircuitEditor.Core.Repositories;
using CommunityToolkit.Mvvm.Input;

namespace CircuitEditor.App.ViewModels;

public sealed partial class ChipDocumentationViewModel(ChipViewModel editor, IDialogService? dialogs,
    IExportRepository? exports, IChipPinoutRenderer? renderer)
{
    private const string MarkdownFilter = "Markdown (*.md)|*.md";

    [RelayCommand]
    private void PinoutPng() => Write("PNG (*.png)|*.png", ".png", () =>
        renderer?.Render(editor.Definition) ?? throw new InvalidOperationException("Экспорт PNG недоступен."));

    [RelayCommand]
    private void PinoutMarkdown() => Write(MarkdownFilter, ".md", () =>
        Encoding.UTF8.GetBytes(ChipMarkdown.Pinout(editor.Definition)));

    [RelayCommand]
    private void TruthMarkdown()
    {
        if (editor.Definition.Behavior is not ChipTruthTable table) return;
        Write(MarkdownFilter, ".md", () => Encoding.UTF8.GetBytes(ChipMarkdown.Truth(editor.Definition, table)));
    }

    private void Write(string filter, string extension, Func<byte[]> create)
    {
        if (exports is null || !editor.CheckReady() || dialogs?.SaveFile(filter, extension, editor.Definition.Name) is not { } path)
            return;
        try
        {
            if (exports.Write(path, create()) is { } error) dialogs.ShowError(error);
            else editor.Message = "Документация экспортирована.";
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or NotSupportedException or OverflowException)
        {
            dialogs.ShowError(error.Message);
        }
    }
}
