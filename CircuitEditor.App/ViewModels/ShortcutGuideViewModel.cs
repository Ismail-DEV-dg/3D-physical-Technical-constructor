using CircuitEditor.App.Interaction;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace CircuitEditor.App.ViewModels;
public sealed partial class ShortcutGuideViewModel : ObservableObject
{
    private readonly IReadOnlyList<ShortcutBinding> entries=ShortcutCatalog.Bindings.Concat(new[]
    {
        new ShortcutBinding("CodeFind",Key.None,0,"Ctrl+F / Ctrl+H","Код","Поиск / замена в редакторе (при фокусе во вкладке «Код»)",false,true),
        new ShortcutBinding("CodeLine",Key.None,0,"Ctrl+G","Код","Перейти к строке в редакторе",false,true),
        new ShortcutBinding("CodeCompletion",Key.None,0,"Ctrl+Space","Код","Дополнение Arduino API и имён проекта",false,true),
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
    }).ToArray();
    [ObservableProperty] private string search="";
    public IReadOnlyList<ShortcutBinding> Entries=>entries.Where(e=>string.IsNullOrWhiteSpace(Search)||
        (e.Keys+" "+e.Description+" "+e.Group).Contains(Search,StringComparison.OrdinalIgnoreCase)).ToArray();
    partial void OnSearchChanged(string value)=>OnPropertyChanged(nameof(Entries));
    public event EventHandler? CloseRequested;
    [RelayCommand] private void Close()=>CloseRequested?.Invoke(this,EventArgs.Empty);
}
