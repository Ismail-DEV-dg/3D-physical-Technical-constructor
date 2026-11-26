using System.Text.RegularExpressions;
namespace CircuitEditor.Core.Code;
/// <summary>Обновляет только явно размеченные секции, сохраняя ручной текст буквально.</summary>
public static class AutoRegionUpdater
{
    private static readonly Regex Regions=new(@"(?m)^(?<indent>[ \t]*)// <auto>\r?\n[ \t]*// section: (?<name>[a-z]+)\r?\n(?<body>[\s\S]*?)^[ \t]*// </auto>[ \t]*(?=\r?$)");
    /// <summary>Заменяет три известные секции либо создаёт новый пустой каркас.</summary>
    public static string Update(string text, IReadOnlyDictionary<string,string> sections)
    {
        if(string.IsNullOrWhiteSpace(text)||text==SketchDocument.StarterText)text="// <auto>\n// section: globals\n// </auto>\n\nvoid setup() {\n  // <auto>\n  // section: setup\n  // </auto>\n}\n\nvoid loop() {\n  // <auto>\n  // section: loop\n  // </auto>\n}\n";
        var matches=Regions.Matches(text);
        if(matches.Count!=sections.Count || matches.Select(m=>m.Groups["name"].Value).Distinct().Count()!=sections.Count
            || matches.Any(m=>!sections.ContainsKey(m.Groups["name"].Value))
            || Regex.Matches(text,@"(?m)^[ \t]*// <auto>[ \t]*\r?$").Count!=sections.Count
            || Regex.Matches(text,@"(?m)^[ \t]*// </auto>[ \t]*\r?$").Count!=sections.Count)
            throw new ArgumentException(text.Contains("<auto>",StringComparison.Ordinal)||text.Contains("</auto>",StringComparison.Ordinal)
                ? "Повреждены секции <auto>: нужны ровно три пары с section: globals/setup/loop. Ручной код не изменён. Способ исправления — в справке по ошибке."
                : "Ручной скетч не содержит секций <auto>. Ручной код не изменён.");
        return Regions.Replace(text,m=>
        {
            var body=m.Groups["body"];var newline=m.Value.Contains("\r\n",StringComparison.Ordinal)?"\r\n":"\n";
            var content=sections[m.Groups["name"].Value].Replace("\r","").Split('\n').Where(l=>l.Length>0)
                .Select(l=>m.Groups["indent"].Value+l);
            var offset=body.Index-m.Index;
            return m.Value[..offset]+string.Join(newline,content)+newline+m.Value[(offset+body.Length)..];
        });
    }
}
