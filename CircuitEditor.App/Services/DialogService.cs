using System.Windows;
using Microsoft.Win32;
using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Views;
namespace CircuitEditor.App.Services;
public sealed class DialogService(IClipboardService? clipboard=null,IErrorHelpService? help=null,IErrorDocumentationService? documentation=null) : IDialogService
{
    public bool OfferCrashCopy(bool journalWritten) =>
        Choose("Произошла ошибка",
            "Операция прервана. Вы можете продолжить работу или сохранить отдельную копию и закрыть программу. " +
            (journalWritten ? "Диагностика записана в журнал." : "Журнал сейчас недоступен."),
            "Продолжить", "Сохранить копию проекта и закрыть") == "Сохранить копию проекта и закрыть";

    public void ShowError(string message) => Choose("Ошибка", message, "ОК");
    public void ShowWarning(string message) => Choose("Предупреждение", message, "ОК");
    public string? OpenProject() => OpenFile("Circuit Editor (*.circ)|*.circ");
    public string? OpenFile(string filter)
    {
        var dialog = new OpenFileDialog { Filter = filter, CheckFileExists = true };
        return dialog.ShowDialog(Application.Current.MainWindow) == true ? dialog.FileName : null;
    }
    public string? SaveFile(string filter, string extension, string? suggested = null)
    {
        var dialog = new SaveFileDialog { Filter = filter, DefaultExt = extension, AddExtension = true, FileName = suggested ?? "Новый проект" };
        return dialog.ShowDialog(Application.Current.MainWindow) == true ? dialog.FileName : null;
    }
    private string? Choose(string title, string message, params string[] choices)
    {
        string? result = null;
        var window = new ChoiceDialog { Owner = Application.Current.MainWindow };
        window.DataContext = new ChoiceDialogViewModel(title, message, choices, choice => { result = choice; window.DialogResult = true; },clipboard,help,documentation, () => window.DialogResult = false);
        window.ShowDialog(); return result;
    }
    public SaveDecision ConfirmUnsaved() => Choose("Несохранённый проект", "Сохранить изменения перед продолжением?", "Сохранить", "Не сохранять", "Отмена") switch
    { "Сохранить" => SaveDecision.Save, "Не сохранять" => SaveDecision.Discard, _ => SaveDecision.Cancel };
    public bool ConfirmDeleteChip(string name) => Choose("Удаление микросхемы",
        $"Удалить «{name}» из пользовательской библиотеки?", "Удалить", "Отмена") == "Удалить";
    public bool UseLocalChipVersion(string name, int embeddedRevision, int localRevision) =>
        Choose("Версия микросхемы", $"«{name}»: встроенная ревизия {embeddedRevision}, локальная {localRevision}. Копии различаются. Какую использовать?",
            "Использовать встроенную", "Обновить из библиотеки") == "Обновить из библиотеки";
    public bool ConfirmRecovery() => Choose("Восстановление", "Найдено автосохранение после незавершённого сеанса. Восстановить проект?", "Восстановить", "Пропустить") == "Восстановить";
    public void ShowAbout() => Choose("О программе", AboutInformation.Describe(), "ОК");

    public double? ChooseExportScale() => ChooseExportScale(false);

    public double? ChooseExportScale(bool isThreeD) => Choose("Экспорт PNG", isThreeD
        ? "Экспортируется вид 3D с текущей камерой и видимыми слоями. Выберите масштаб PNG."
        : "Экспортируется весь проект в активном представлении. Выберите масштаб PNG.",
        "1×", "2×", "4×", "Отмена") switch
    { "1×" => 1, "2×" => 2, "4×" => 4, _ => null };
}
