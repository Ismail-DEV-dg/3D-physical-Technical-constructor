namespace CircuitEditor.Core.Electronics;
/// <summary>Любая пара независимых величин U, I, R, P для пассивного сопротивления.</summary>
public static class OhmCalculator
{
    /// <summary>Решает положительную ветвь; недоопределённые нулевые пары отклоняются.</summary>
    public static CalculationResult<OhmValues> Solve(double? u=null,double? i=null,double? r=null,double? p=null)
    {
        var given=new[]{u,i,r,p}.Where(v=>v.HasValue).Select(v=>v!.Value).ToArray();
        if(given.Length!=2 || given.Any(v=>!double.IsFinite(v)||v<0) || r is <=0) return new(null,"Введите ровно две неотрицательные величины; сопротивление должно быть положительным.");
        if(u.HasValue && i.HasValue){if(i==0)return new(null,"При I=0 сопротивление не определяется этой парой.");r=u/i;}
        else if(u.HasValue && r.HasValue)i=u/r;
        else if(u.HasValue && p.HasValue){if(u==0)return new(null,"При U=0 эта пара не определяет ток.");i=p/u;r=u/i;}
        else if(i.HasValue && r.HasValue)u=i*r;
        else if(i.HasValue && p.HasValue){if(i==0)return new(null,"При I=0 эта пара не определяет сопротивление.");r=p/(i*i);u=i*r;}
        else if(r.HasValue && p.HasValue){i=Math.Sqrt(p.Value/r.Value);u=i*r;}
        p=u*i;
        if(u is null || i is null || r is null || p is null || r<=0 || new[]{u.Value,i.Value,r.Value,p.Value}.Any(v=>!double.IsFinite(v)))return new(null,"Величины несовместимы или результат не определён.");
        return new(new(u.Value,i.Value,r.Value,p.Value),null);
    }
}
