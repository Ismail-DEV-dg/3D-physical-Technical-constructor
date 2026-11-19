namespace CircuitEditor.Core.Code;
/// <summary>Распознаёт только известные API и простые аргументы; не является C++-парсером.</summary>
public static class PinUsageCollector
{
    /// <summary>Извлекает вызовы API, не принимая посторонние числа за пины.</summary>
    public static IReadOnlyList<CodePinUse> Collect(string text,CodeBoardProfile profile,IReadOnlyList<CodeCompletionItem> api)
    {
        var tokens=CppTokenScanner.Scan(text);var aliases=new Dictionary<string,string>();
        var preprocessorLines=new HashSet<int>();
        var continuation=false;
        foreach(var line in tokens.GroupBy(t=>t.Line))
        {
            var parts=line.ToArray();
            if(parts[0].Text=="#"||continuation){preprocessorLines.Add(parts[0].Line);continuation=parts[^1].Text=="\\";}
            if(parts.Length>=4&&parts[0].Text=="#"&&parts[1].Text=="define"
                && (parts[3].Text!="("||parts[3].Offset>parts[2].Offset+parts[2].Text.Length)
                && SimpleValue(parts.Skip(3).ToArray()) is { } value)aliases[parts[2].Text]=value;
        }
        for(var i=1;i+2<tokens.Count;i++)
        {
            if(tokens[i].Text!="=")continue;var isConstant=false;
            for(var j=i-1;j>=0&&tokens[j].Text is not (";" or "{" or "}");j--)
                if(tokens[j].Text is "const" or "constexpr"){isConstant=true;break;}
            var end=i+1;while(end<tokens.Count&&tokens[end].Text!=";")end++;
            var value=SimpleValue(tokens.Skip(i+1).Take(end-i-1).ToArray());
            if(isConstant&&value is not null)aliases[tokens[i-1].Text]=value;else aliases.Remove(tokens[i-1].Text);
        }
        var result=new List<CodePinUse>();
        for(var i=0;i<tokens.Count-1;i++)
        {
            if(preprocessorLines.Contains(tokens[i].Line))continue;
            var qualified=i>=2&&tokens[i-1].Text=="." ? tokens[i-2].Text+"."+tokens[i].Text : tokens[i].Text;
            var entry=api.FirstOrDefault(a=>a.Text==qualified&&a.PinArguments.Count>0)
                ?? api.FirstOrDefault(a=>a.Text==tokens[i].Text&&a.PinArguments.Count>0);if(entry is null)continue;
            if(i>=2&&tokens[i-1].Text=="."&&!entry.Text.Contains('.')&&entry.Text!="attach")continue;
            if(entry.Text=="attach"&&(i<2||tokens[i-1].Text!="."||!tokens.Zip(tokens.Skip(1)).Any(p=>p.First.Text=="Servo"&&p.Second.Text==tokens[i-2].Text)))continue;
            var open=i+1;
            if(entry.Text=="DHT"&&open+1<tokens.Count&&tokens[open+1].Text=="(")open++;
            if(tokens[open].Text!="(")continue;
            var args=new List<List<CppToken>>{new()};var depth=0;
            for(var k=open+1;k<tokens.Count;k++)
            {
                var token=tokens[k];if(token.Text==")"&&depth==0)break;
                if(token.Text==","&&depth==0){args.Add([]);continue;}
                if(token.Text=="(")depth++;if(token.Text==")")depth--;args[^1].Add(token);
            }
            if(entry.Text=="Wire.begin"&&(args.Count==1&&args[0].Count==0 || profile.Kind!=BoardKind.Esp32))
            {
                foreach(var number in new[]{profile.DefaultSdaNumber,profile.DefaultSclNumber})
                    if(number is not null&&profile.Pins.FirstOrDefault(p=>p.Number==number) is { } pin)
                        result.Add(new(pin,entry.Text,tokens[i].Line,tokens[i].Column,false));
                continue;
            }
            foreach(var position in entry.PinArguments)
            {
                if(position>=args.Count||SimpleValue(args[position]) is not { } expression)continue;
                var token=args[position][0];var pin=CodePinResolver.Resolve(expression,profile,aliases);if(pin is null)continue;
                var mode=args.Count>1&&args[1].Count==1 ? args[1][0].Text : "";
                var seen=new HashSet<string>();while(aliases.TryGetValue(mode,out var replacement)&&seen.Add(mode))mode=replacement;
                result.Add(new(pin,entry.Text,token.Line,token.Column,entry.Writes||entry.SetsMode&&mode=="OUTPUT"));
            }
        }
        return result;
    }
    private static string? SimpleValue(IReadOnlyList<CppToken> tokens)
    {
        var start=0;var end=tokens.Count-1;
        while(end>start&&tokens[start].Text=="("&&tokens[end].Text==")"){start++;end--;}
        return start==end&&tokens[start].Text.All(c=>char.IsLetterOrDigit(c)||c=='_') ? tokens[start].Text : null;
    }
}
