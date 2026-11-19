namespace CircuitEditor.Core.Code;
/// <summary>Добавляет пустые секции генератора в простые setup/loop, сохраняя ручной код.</summary>
public static class AutoRegionInitializer
{
    /// <summary>Подготавливает только однозначный каркас; повреждённые существующие маркеры не заменяет.</summary>
    public static CodePreparationResult Prepare(string text)
    {
        CodePreparationResult Fail(string message)=>new(false,text,message);
        if(text.Contains("<auto>",StringComparison.Ordinal)||text.Contains("</auto>",StringComparison.Ordinal))
            return Fail("В тексте уже есть маркеры <auto>. Проверьте три секции globals/setup/loop в справке; существующий код не изменён.");
        if(string.IsNullOrWhiteSpace(text))return new(true,AutoRegionUpdater.Update(text,new Dictionary<string,string>{{"globals",""},{"setup",""},{"loop",""}}),"Каркас подготовлен.");
        var tokens=CppTokenScanner.Scan(text);var insertions=new List<(int Offset,string Section)>();var depth=0;
        for(var i=0;i+4<tokens.Count;i++)
        {
            if(depth==0&&tokens[i].Text=="void"&&tokens[i+1].Text is "setup" or "loop"
                && tokens[i+2].Text=="("&&tokens[i+3].Text==")"&&tokens[i+4].Text=="{")
                insertions.Add((tokens[i+4].Offset+1,tokens[i+1].Text));
            if(tokens[i].Text=="{")depth++;else if(tokens[i].Text=="}")depth--;
        }
        if(insertions.Count!=2||insertions.Select(p=>p.Section).Distinct().Count()!=2
            || tokens.Count(t=>t.Text=="{")!=tokens.Count(t=>t.Text=="}"))
            return Fail("Подготовка требует единственных void setup() и void loop() с целыми фигурными скобками. Код оставлен без изменений.");
        var newline=text.Contains("\r\n",StringComparison.Ordinal)?"\r\n":"\n";
        foreach(var insertion in insertions.OrderByDescending(i=>i.Offset))
            text=text.Insert(insertion.Offset,$"{newline}  // <auto>{newline}  // section: {insertion.Section}{newline}  // </auto>{newline}");
        text=$"// <auto>{newline}// section: globals{newline}// </auto>{newline}{newline}"+text;
        return new(true,text,"Пустые секции добавлены. Ручные инструкции сохранены; теперь можно нажать «Обновить».");
    }
}
