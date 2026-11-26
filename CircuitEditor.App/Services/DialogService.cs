using System.IO;
using System.Windows;
using CircuitEditor.Core.Repositories;
using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Views;
namespace CircuitEditor.App.Services;
public sealed class DialogService(IClipboardService? clipboard=null,IErrorHelpService? help=null,IErrorDocumentationService? documentation=null, IInstallationLicenseRepository? license = null) : IDialogService
{
    private string? pickerDirectory;

    public bool OfferCrashCopy(bool journalWritten) =>
        Choose("Произошла ошибка",
            "Операция прервана. Вы можете продолжить работу или сохранить отдельную копию и закрыть программу. " +
            (journalWritten ? "Диагностика записана в журнал." : "Журнал сейчас недоступен."),
            "Продолжить", "Сохранить копию проекта и закрыть") == "Сохранить копию проекта и закрыть";

    public void ShowError(string message) => Choose("Ошибка", message, "ОК");
    public void ShowWarning(string message) => Choose("Предупреждение", message, "ОК");
    public string? OpenProject() => OpenFile("Circuit Editor (*.circ)|*.circ");
    public string? OpenFile(string filter) => PickFile(filter, null, null);
    public string? SaveFile(string filter, string extension, string? suggested = null) =>
        PickFile(filter, extension, suggested ?? "Новый проект");

    private string? PickFile(string filter, string? extension, string? suggested)
    {
        string? selectedPath = null;
        var window = new FilePickerWindow { Owner = Application.Current.MainWindow };
        window.DataContext = new FilePickerViewModel(filter, extension, suggested, pickerDirectory,
            path => { selectedPath = path; window.DialogResult = path is not null; },
            path => ChooseOwned(window, "Замена файла", $"Файл «{Path.GetFileName(path)}» уже существует. Заменить его?",
                "Заменить", "Отмена") == "Заменить");
        if (window.ShowDialog() != true || selectedPath is null)
            return null;

        pickerDirectory = Path.GetDirectoryName(selectedPath);
        return selectedPath;
    }

    private string? Choose(string title, string message, params string[] choices) =>
        ChooseOwned(Application.Current.MainWindow, title, message, choices);

    private string? ChooseOwned(Window? owner, string title, string message, params string[] choices)
    {
        string? result = null;
        var window = new ChoiceDialog { Owner = owner };
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
    public void ShowAbout()
    {
        if (Choose("О программе", AboutInformation.Describe(), "ОК", "Открыть LICENSE…") != "Открыть LICENSE…") return;
        var result = license?.Read();
        if (result?.Value is { } text) Choose("Лицензия MIT", text, "ОК");
        else ShowError(result?.Error ?? "Файл LICENSE недоступен.");
    }

    public double? ChooseExportScale() => Choose("Экспорт PNG", "Экспортируется весь проект в активном представлении. Выберите масштаб PNG.",
        "1×", "2×", "4×", "Отмена") switch
    { "1×" => 1, "2×" => 2, "4×" => 4, _ => null };
}
