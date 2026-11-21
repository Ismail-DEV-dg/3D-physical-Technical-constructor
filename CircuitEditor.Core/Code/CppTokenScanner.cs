namespace CircuitEditor.Core.Code;
/// <summary>Небольшой C++-лексер для анализа API и безопасного сворачивания скобок.</summary>
public static class CppTokenScanner
{
    private const int MaximumRawDelimiterLength=16;
    private static readonly string[] RawPrefixes=["u8R\"","uR\"","UR\"","LR\"","R\""];
    /// <summary>Сохраняет позиции лексем. Строки и символьные литералы включаются по запросу.</summary>
    public static IReadOnlyList<CppToken> Scan(string text, bool includeLiterals = false)
    {
        var result=new List<CppToken>();var i=0;var line=1;var column=1;
        void Advance(){if(text[i++]=='\n'){line++;column=1;}else column++;}
        bool At(string value)=>text.AsSpan(i).StartsWith(value,StringComparison.Ordinal);
        while(i<text.Length)
        {
            var start=i;var startColumn=column;var startLine=line;
            if(char.IsWhiteSpace(text[i])){Advance();continue;}
            if(At("//"))
            {
                while(i<text.Length&&text[i]!='\n')
                {
                    if(At("\\\r\n")){Advance();Advance();Advance();}
                    else if(At("\\\n")){Advance();Advance();}else Advance();
                }
                continue;
            }
            if(At("/*"))
            {
                Advance();Advance();while(i<text.Length&&!At("*/"))Advance();
                if(i<text.Length){Advance();Advance();}
                else if(includeLiterals)result.Add(new("/*",start,startLine,startColumn));
                continue;
            }
            if(RawPrefixes.FirstOrDefault(At) is { } rawPrefix)
            {
                var delimiterStart=i+rawPrefix.Length;var open=text.IndexOf('(',delimiterStart);
                if(open>=0&&open-delimiterStart<=MaximumRawDelimiterLength)
                {
                    var end=")"+text[delimiterStart..open]+"\"";while(i<=open)Advance();
                    while(i<text.Length&&!At(end))Advance();
                    if(i<text.Length)for(var n=0;n<end.Length;n++)Advance();
                    if(includeLiterals)result.Add(new(text[start..i],start,startLine,startColumn));
                    continue;
                }
            }
            if(text[i] is '\"' or '\'')
            {
                var quote=text[i];Advance();
                while(i<text.Length)
                {
                    if(text[i]=='\\'){Advance();if(i<text.Length)Advance();continue;}
                    if(text[i]==quote){Advance();break;}Advance();
                }
                if(includeLiterals)result.Add(new(text[start..i],start,startLine,startColumn));
                continue;
            }
            if(char.IsLetterOrDigit(text[i])||text[i]=='_')
                while(i<text.Length&&(char.IsLetterOrDigit(text[i])||text[i]=='_'))Advance();
            else Advance();
            result.Add(new(text[start..i],start,startLine,startColumn));
        }
        return result;
    }
}
