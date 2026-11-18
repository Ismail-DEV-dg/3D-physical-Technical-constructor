using System.Windows;
using Microsoft.Win32;
using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Views;
namespace CircuitEditor.App.Services;
public sealed class DialogService : IDialogService
{
    public void ShowError(string message) => Choose("Ошибка", message, "ОК");
    public void ShowWarning(string message) => Choose("Предупреждение", message, "ОК");
    public string? OpenProject()
    {
        var dialog = new OpenFileDialog { Filter = "Circuit Editor (*.circ)|*.circ", CheckFileExists = true };
        return dialog.ShowDialog(Application.Current.MainWindow) == true ? dialog.FileName : null;
    }
    public string? SaveFile(string filter, string extension, string? suggested = null)
    {
        var dialog = new SaveFileDialog { Filter = filter, DefaultExt = extension, AddExtension = true, FileName = suggested ?? "Новый проект" };
        return dialog.ShowDialog(Application.Current.MainWindow) == true ? dialog.FileName : null;
    }
    private static string? Choose(string title, string message, params string[] choices)
    {
        string? result = null;
        var window = new ChoiceDialog { Owner = Application.Current.MainWindow };
        window.DataContext = new ChoiceDialogViewModel(title, message, choices, choice => { result = choice; window.DialogResult = true; });
        window.ShowDialog(); return result;
    }
    public SaveDecision ConfirmUnsaved() => Choose("Несохранённый проект", "Сохранить изменения перед продолжением?", "Сохранить", "Не сохранять", "Отмена") switch
    { "Сохранить" => SaveDecision.Save, "Не сохранять" => SaveDecision.Discard, _ => SaveDecision.Cancel };
    public bool ConfirmRecovery() => Choose("Восстановление", "Найдено автосохранение после незавершённого сеанса. Восстановить проект?", "Восстановить", "Пропустить") == "Восстановить";
    public double? ChooseExportScale() => Choose("Экспорт PNG", "Масштаб изображения относительно 100% вида. Экспортируется весь проект.", "1×", "2×", "4×", "Отмена") switch
    { "1×" => 1, "2×" => 2, "4×" => 4, _ => null };
}
