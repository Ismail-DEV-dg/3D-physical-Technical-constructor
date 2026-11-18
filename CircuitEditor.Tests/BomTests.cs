using System.Text;
using CircuitEditor.Core.Export;
namespace CircuitEditor.Tests;
public sealed class BomTests
{
    [Fact]
    public void SameNominalsGroupAndDifferentParametersRemainSeparateWithExcelEncoding()
    {
        using var e=new Stage4TestEnvironment(); var p=e.Project; var definition=p.Library.Single(d=>d.Id=="resistor");
        p.Add(definition); p.Add(definition); var third=p.Add(definition)!;
        third.Model.TryUpdateProperties("470 Ω",null,null,out _);
        var service=new BomExportService(e); var bytes=service.Create(p.Model);
        Assert.True(bytes.Take(3).SequenceEqual(new byte[]{0xef,0xbb,0xbf}));
        var csv=Encoding.UTF8.GetString(bytes[3..]); var lines=csv.Split("\r\n",StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3,lines.Length); Assert.Contains("R1, R2",lines[1]); Assert.EndsWith(";2",lines[1]); Assert.Contains("470 Ω",lines[2]);
        Assert.Null(service.Export("test.csv",p.Model)); Assert.Equal(bytes,e.Exported);
    }
}
