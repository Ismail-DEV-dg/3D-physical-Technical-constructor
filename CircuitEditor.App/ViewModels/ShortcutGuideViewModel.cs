using CircuitEditor.App.Interaction;
using CircuitEditor.App.Services;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace CircuitEditor.App.ViewModels;
public sealed partial class ShortcutGuideViewModel : ObservableObject
{
    private readonly IShortcutSettings settings;
    private static readonly ShortcutBinding[] FixedGestures =
    [
        new ShortcutBinding("CodeZoom",Key.None,0,"Ctrl+колесо","Код","Изменить размер шрифта редактора",false,true),
        new ShortcutBinding("CodeUndo",Key.None,0,"Ctrl+Z / Ctrl+Y / Ctrl+Shift+Z","Код","Независимая история текста; история схемы не меняется",false,true),
        new ShortcutBinding("Mouse",Key.None,0,"ЛКМ + движение","Мышь","Переместить компонент; breadboard тяните за свободную кромку",true),
        new ShortcutBinding("Mouse",Key.None,0,"Ctrl + ЛКМ","Мышь","Добавить / убрать компонент из выделения",true),
        new ShortcutBinding("Mouse",Key.None,0,"ЛКМ по пустому месту + движение","Мышь","Рамка выделения в «Схеме»",true),
        new ShortcutBinding("Mouse",Key.None,0,"Колесо","Мышь","Масштаб вокруг указателя",true),
        new ShortcutBinding("Mouse",Key.None,0,"СКМ / ПКМ + движение","Мышь","Панорамирование",true),
        new ShortcutBinding("Mouse",Key.None,0,"Space + ЛКМ + движение","Мышь","Панорамирование",true),
        new ShortcutBinding("Mouse",Key.None,0,"Два клика по контактам","Перемычки","Создать перемычку при включённом W; Esc отменяет",true),
        new ShortcutBinding("Mouse",Key.None,0,"Потянуть выбранную линию / кружок","Перемычки","Добавить изгиб / переместить узел или конец",true)
    ];
    public ShortcutGuideViewModel(IShortcutSettings? settings = null) =>
        this.settings = settings ?? new ShortcutSettings();

    [ObservableProperty] private string search = "";
    [ObservableProperty] private string feedback = "";
    [ObservableProperty] private ShortcutBinding? selected;
    public bool CanChange => Selected?.CanCustomize == true;

    public IReadOnlyList<ShortcutBinding> Entries => settings.Bindings.Concat(FixedGestures)
        .Where(entry => (entry.Keys + " " + entry.Description + " " + entry.Group)
            .Contains(Search.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();

    partial void OnSearchChanged(string value) => OnPropertyChanged(nameof(Entries));
    partial void OnSelectedChanged(ShortcutBinding? value)
    {
        OnPropertyChanged(nameof(CanChange));
        ResetCommand.NotifyCanExecuteChanged();
    }

    internal void Assign(Key key, ModifierKeys modifiers)
    {
        if (Selected is not { } binding) return;
        Update(binding, settings.Assign(binding, key, modifiers), "Сочетание сохранено.");
    }

    [RelayCommand(CanExecute = nameof(CanChange))]
    private void Reset()
    {
        if (Selected is { } binding) Update(binding, settings.Restore(binding), "Сочетание по умолчанию восстановлено.");
    }

    [RelayCommand]
    private void ResetAll() => Update(Selected, settings.RestoreAll(), "Все сочетания по умолчанию восстановлены.");

    private void Update(ShortcutBinding? binding, string? error, string success)
    {
        Feedback = error ?? success;
        if (error is not null) return;
        var entries = Entries;
        OnPropertyChanged(nameof(Entries));
        Selected = entries.FirstOrDefault(entry => entry.Id == binding?.Id && entry.Keys == binding.Keys)
            ?? entries.FirstOrDefault(entry => entry.Id == binding?.Id);
    }

    public event EventHandler? CloseRequested;
    [RelayCommand] private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
