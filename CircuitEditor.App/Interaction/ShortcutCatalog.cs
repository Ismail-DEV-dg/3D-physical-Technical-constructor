using System.Windows.Input;
namespace CircuitEditor.App.Interaction;
public static class ShortcutCatalog
{
    public static IReadOnlyList<ShortcutBinding> Bindings {get;} = Build();
    private static IReadOnlyList<ShortcutBinding> Build()
    {
        var list=new List<ShortcutBinding>();
        void Add(string id,Key key,ModifierKeys mods,string keys,string group,string description,bool canvas=false,bool text=false)=>list.Add(new(id,key,mods,keys,group,description,canvas,text));
        const ModifierKeys Ctrl=ModifierKeys.Control,Shift=ModifierKeys.Shift,Alt=ModifierKeys.Alt;
        Add("Help",Key.F1,0,"F1","Справка","Список горячих клавиш",text:true);
        Add("Search",Key.F,Ctrl,"Ctrl+F","Навигация","Поиск компонента в палитре",text:true);
        Add("New",Key.N,Ctrl,"Ctrl+N","Проект","Создать проект",text:true);
        Add("Open",Key.O,Ctrl,"Ctrl+O","Проект","Открыть проект",text:true);
        Add("Save",Key.S,Ctrl,"Ctrl+S","Проект","Сохранить",text:true);
        Add("SaveAs",Key.S,Ctrl|Shift,"Ctrl+Shift+S","Проект","Сохранить как…",text:true);
        Add("Undo",Key.Z,Ctrl,"Ctrl+Z","Правка","Отменить действие");
        Add("Redo",Key.Y,Ctrl,"Ctrl+Y","Правка","Повторить действие");
        Add("Redo",Key.Z,Ctrl|Shift,"Ctrl+Shift+Z","Правка","Повторить действие");
        Add("Copy",Key.C,Ctrl,"Ctrl+C","Правка","Копировать выбранные компоненты");
        Add("Paste",Key.V,Ctrl,"Ctrl+V","Правка","Вставить копию вне breadboard");
        Add("Duplicate",Key.D,Ctrl,"Ctrl+D","Правка","Дублировать выбранные компоненты",true);
        Add("SelectAll",Key.A,Ctrl,"Ctrl+A","Холст","Выделить все компоненты",true);
        Add("Rotate",Key.R,0,"R","Холст","Повернуть компонент или выбранную breadboard",true);
        Add("Delete",Key.Delete,0,"Del","Холст","Удалить выбранные компоненты или перемычку",true);
        Add("Cancel",Key.Escape,0,"Esc","Холст","Отменить жест / выйти из прокладки проводов",true);
        Add("Select",Key.V,0,"V","Холст","Вернуться к выбору и перемещению",true);
        Add("Wire",Key.W,0,"W","Холст","Включить / выключить перемычки в «Макете»",true);
        Add("Fit",Key.F,0,"F","Вид","Вписать весь проект в активный холст",true);
        Add("Fit",Key.Home,0,"Home","Вид","Вписать весь проект",true);
        Add("Reset",Key.D0,0,"0","Вид","Сбросить вид и масштаб до 100%",true);
        Add("ZoomIn",Key.OemPlus,0,"=","Вид","Увеличить масштаб вокруг центра холста",true);
        Add("ZoomIn",Key.OemPlus,Shift,"+","Вид","Увеличить масштаб",true);
        Add("ZoomIn",Key.Add,0,"Num +","Вид","Увеличить масштаб",true);
        Add("ZoomOut",Key.OemMinus,0,"−","Вид","Уменьшить масштаб вокруг центра холста",true);
        Add("ZoomOut",Key.Subtract,0,"Num −","Вид","Уменьшить масштаб",true);
        Add("Check",Key.F5,0,"F5","Расчёты","Проверить схему (ERC)");
        Add("Calculate",Key.F6,0,"F6","Расчёты","Рассчитать цепь DC");
        Add("Calculator",Key.K,Ctrl,"Ctrl+K","Расчёты","Открыть калькулятор");
        Add("Theme",Key.T,Ctrl|Shift,"Ctrl+Shift+T","Вид","Переключить тему");
        Add("NextTab",Key.Tab,Ctrl,"Ctrl+Tab","Навигация","Следующая вкладка",text:true);
        Add("PreviousTab",Key.Tab,Ctrl|Shift,"Ctrl+Shift+Tab","Навигация","Предыдущая вкладка",text:true);
        for(var i=0;i<5;i++)Add("Tab"+i,(Key)((int)Key.D0+i),Ctrl,"Ctrl+"+i,"Навигация",new[]{"Макет","Схема","Микросхема","3D","Код"}[i],text:true);
        Add("Png",Key.P,Ctrl|Alt,"Ctrl+Alt+P","Экспорт","Экспорт PNG активного вида");
        Add("Svg",Key.S,Ctrl|Alt,"Ctrl+Alt+S","Экспорт","Экспорт SVG схемы");
        Add("Bom",Key.B,Ctrl|Alt,"Ctrl+Alt+B","Экспорт","Экспорт ведомости CSV");
        return list.AsReadOnly();
    }
    public static ShortcutBinding? Resolve(Key key,ModifierKeys modifiers,bool editingText,bool canvasFocused)=>Bindings.FirstOrDefault(b=>b.Key==key&&b.Modifiers==modifiers&&(!editingText||b.AllowText)&&(!b.CanvasOnly||canvasFocused));
    public static string Hint(string id)
    {var items=Bindings.Where(b=>b.Id==id).ToArray();return items.Length==0 ? "" : items[0].Description+" · "+string.Join(" / ",items.Select(b=>b.Keys))+(items[0].CanvasOnly?" (на холсте)":"");}
}
