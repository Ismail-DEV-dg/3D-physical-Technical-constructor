namespace CircuitEditor.Core.Electronics;
/// <summary>Исключение Гаусса с выбором главного элемента; вырожденность возвращается как отказ.</summary>
public static class LinearSystemSolver
{
    private const double PivotTolerance=1e-12;
    /// <summary>Решает Ax=b, не изменяя исходные массивы.</summary>
    public static double[]? Solve(double[,] coefficients,double[] right)
    {
        var a=(double[,])coefficients.Clone();var b=(double[])right.Clone();var n=b.Length;
        for(var column=0;column<n;column++)
        {
            var pivot=column;for(var row=column+1;row<n;row++)if(Math.Abs(a[row,column])>Math.Abs(a[pivot,column]))pivot=row;
            if(!double.IsFinite(a[pivot,column])||Math.Abs(a[pivot,column])<PivotTolerance)return null;
            for(var k=column;k<n;k++)(a[pivot,k],a[column,k])=(a[column,k],a[pivot,k]);(b[pivot],b[column])=(b[column],b[pivot]);
            for(var row=column+1;row<n;row++)
            {
                var factor=a[row,column]/a[column,column];
                for(var k=column;k<n;k++)a[row,k]-=factor*a[column,k];b[row]-=factor*b[column];
            }
        }
        var result=new double[n];
        for(var row=n-1;row>=0;row--){var sum=b[row];for(var k=row+1;k<n;k++)sum-=a[row,k]*result[k];result[row]=sum/a[row,row];if(!double.IsFinite(result[row]))return null;}
        return result;
    }
}
