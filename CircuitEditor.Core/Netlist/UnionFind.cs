namespace CircuitEditor.Core.Netlist;
internal sealed class UnionFind
{
    private readonly Dictionary<string,string> parents=[];
    internal string Find(string key)
    {
        if(!parents.TryGetValue(key,out var parent)) return parents[key]=key;
        return parent==key ? key : parents[key]=Find(parent);
    }
    internal void Join(string a,string b)
    { var x=Find(a); var y=Find(b); if(x!=y) parents[x]=y; }
}
