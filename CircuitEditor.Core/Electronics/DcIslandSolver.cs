namespace CircuitEditor.Core.Electronics;
internal static class DcIslandSolver
{
    internal const string IterationLimitMessage = "Кусочно-линейная LED-модель не сошлась за допустимое число итераций.";
    private const int MaximumIterations=64;
    private const double CurrentTolerance=1e-9;
    private const double OpenDiodeConductance = 1e-10;
    private const double VoltageTolerance=1e-8;
    internal static (Dictionary<string,double>? Voltages,List<DcBranchResult> Branches,string? Error) Solve(
        IReadOnlyList<DcBranch> branches,string reference,DcMatrixCache? cache)
    {
        var nodes=branches.SelectMany(b=>new[]{b.A,b.B}).Distinct().Where(n=>n!=reference).ToArray();
        var indices=nodes.Select((n,i)=>(n,i)).ToDictionary(p=>p.n,p=>p.i);
        var sources=branches.Where(b=>b.Kind=="V" && b.A!=b.B).ToArray();
        var size=nodes.Length+sources.Length;var active=branches.Where(b=>b.Kind=="LED").ToDictionary(b=>b.Name,_=>true);
        double[]? solution=null;
        int Index(string node)=>indices.GetValueOrDefault(node,-1);
        double Voltage(string node)=>node==reference?0:solution![Index(node)];
        for(var iteration=0;iteration<MaximumIterations;iteration++)
        {
            // Source voltage and diode offset affect only the right-hand side.
            var key = cache is null ? "" : reference + "|" + string.Join("|", branches.Select(b =>
                FormattableString.Invariant($"{b.A}:{b.B}:{b.Kind}:{(b.Kind is "V" or "I" ? 0 : b.Value):R}:{active.GetValueOrDefault(b.Name)}")));
            double[,]? a = null;
            var stampMatrix = cache?.TryGet(key, out a) != true;
            a ??= new double[size,size];
            var right=new double[size];
            foreach(var branch in branches)
            {
                var x=Index(branch.A);var y=Index(branch.B);
                if(branch.Kind=="V")
                {
                    if(branch.A==branch.B){if(branch.Value!=0)return(null,[],"КЗ идеального источника: вырожденная/несовместимая система.");continue;}
                    var index=nodes.Length+Array.IndexOf(sources,branch);
                    if(stampMatrix){if(x>=0){a[x,index]+=1;a[index,x]+=1;}if(y>=0){a[y,index]-=1;a[index,y]-=1;}}
                    right[index]=branch.Value;
                }
                else if (branch.Kind == "I")
                {
                    if (x >= 0) right[x] -= branch.Value;
                    if (y >= 0) right[y] += branch.Value;
                }
                else if(branch.Kind is "R" or "TH" or "LED")
                {
                    var conducting = branch.Kind != "LED" || active[branch.Name];
                    var g = conducting ? 1 / branch.Value : OpenDiodeConductance;
                    if(!double.IsFinite(g)||g<=0)return(null,[],"Некорректное сопротивление ветви.");
                    if(stampMatrix){if(x>=0)a[x,x]+=g;if(y>=0)a[y,y]+=g;if(x>=0&&y>=0){a[x,y]-=g;a[y,x]-=g;}}
                    if(conducting && branch.Kind is ("LED" or "TH")){if(x>=0)right[x]+=branch.Vf*g;if(y>=0)right[y]-=branch.Vf*g;}
                }
            }
            if(stampMatrix)cache?.Add(key,a);
            solution=LinearSystemSolver.Solve(a,right);
            if(solution is null)return(null,[],"Вырожденная матрица: плавающий узел или несовместимые идеальные источники.");
            var changed=false;
            foreach(var led in branches.Where(b=>b.Kind=="LED"))
            {
                var drop=Voltage(led.A)-Voltage(led.B);
                var next=active[led.Name] ? (drop-led.Vf)/led.Value>=-CurrentTolerance : drop>led.Vf+VoltageTolerance;
                if(next!=active[led.Name]){active[led.Name]=next;changed=true;}
            }
            if(changed)continue;
            var voltages=nodes.ToDictionary(n=>n,Voltage);voltages[reference]=0;
            var results=branches.Select(b=>
            {
                var drop=Voltage(b.A)-Voltage(b.B);
                double? current=b.Kind=="V" ? b.A==b.B ? null : solution[nodes.Length+Array.IndexOf(sources,b)]
                    : b.Kind=="I" ? b.Value : b.Kind=="LED" ? active[b.Name] ? Math.Max(0,(drop-b.Vf)/b.Value):0 : (drop-(b.Kind=="TH"?b.Vf:0))/b.Value;
                return new DcBranchResult(b.ComponentId,b.Name,b.A,b.B,drop,current,drop*current,b.PinA,b.PinB,b.MaximumCurrent);
            }).ToList();
            return(voltages,results,null);
        }
        return(null,[],IterationLimitMessage);
    }
}
