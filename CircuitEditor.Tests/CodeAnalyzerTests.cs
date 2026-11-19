using CircuitEditor.Core.Code;
using CircuitEditor.Core.Electronics;
using CircuitEditor.Core.Models;
using CircuitEditor.Infrastructure.Repositories;
namespace CircuitEditor.Tests;
public sealed class CodeAnalyzerTests
{
    private static IReadOnlyList<CodeProblem> Analyze(ElectricalTestCircuit e,string text,BoardKind kind=BoardKind.Uno)=>
        new ArduinoCodeAnalyzer(new JsonCodeCompletionRepository(),new JsonCodeTemplateRepository()).Analyze(e.Project.Model,e.Nets(),new(text,kind));
    [Fact]
    public void BothDirectionsOfCodeVersusCircuitAreReportedWithLines()
    {
        using var e=new ElectricalTestCircuit();var uno=e.Add("arduino-uno");var led=e.Add("led");e.Wire(uno,"D9",led,"A");
        var problems=Analyze(e,"#define FREE D8\nvoid setup() { digitalWrite(FREE, HIGH); }");
        Assert.Contains(problems,p=>p.Message.Contains("D8")&&p.Line==2&&p.Severity=="Warning");
        Assert.Contains(problems,p=>p.Message.Contains("D9")&&p.Severity=="Info");
        Assert.DoesNotContain(Analyze(e,"constexpr int LED_PIN=9;\nvoid setup(){pinMode(LED_PIN, OUTPUT);}"),p=>p.Message.Contains("D9"));
    }
    [Theory]
    [InlineData("pinMode(GPIO34, OUTPUT);",true)]
    [InlineData("#define P GPIO39\ndigitalWrite(P, HIGH);",true)]
    [InlineData("pinMode(37, OUTPUT);",true)]
    [InlineData("digitalRead(GPIO34);",false)]
    [InlineData("pinMode(GPIO35, INPUT);",false)]
    [InlineData("digitalWrite(18, HIGH);",false)]
    public void Esp32InputOnlyOutputWarningComesFromTextAndBoardTable(string text,bool expected)
    {
        using var e=new ElectricalTestCircuit(); // No controller instance or simulation role is required.
        var problems=Analyze(e,text,BoardKind.Esp32);
        Assert.Equal(expected,problems.Any(p=>p.Message.Contains("только ввод")&&p.Severity=="Warning"));
    }
    [Fact]
    public void CommentsStringsRawStringsAndOrdinaryNumbersDoNotBecomePinUses()
    {
        using var e=new ElectricalTestCircuit();e.Add("esp32-devkit");
        var text="// digitalWrite(34,HIGH);\n/* pinMode(GPIO35, OUTPUT); */\n"+
            "Serial.println(\"digitalWrite(39,HIGH)\");\nconst char* s=R\"tag(pinMode(36, OUTPUT))tag\";\n"+
            "char quote='\\\''; delay(34); int x=39;";
        Assert.Empty(Analyze(e,text,BoardKind.Esp32));
    }
    [Fact]
    public void NumericAnalogNamedAliasesAndKnownInputRolesAreRecognized()
    {
        using var e=new ElectricalTestCircuit();var uno=e.Add("arduino-uno");var pot=e.Add("potentiometer");e.Wire(uno,"A0",pot,"W");
        uno.Model.TrySetElectrical(new ComponentElectricalState(pinRoles:new Dictionary<string,PinRole>{{"A0",PinRole.Input}}));
        var messages=Analyze(e,"#define SENSOR A0\n#define ALIAS SENSOR\nanalogRead(ALIAS); digitalWrite(14,HIGH);");
        Assert.DoesNotContain(messages,p=>p.Message.Contains("не подключён")||p.Message.Contains("не найден"));
        Assert.Contains(messages,p=>p.Message.Contains("роли Input"));
        Assert.DoesNotContain(Analyze(e,"analogRead(A0 + arbitrary());"),p=>p.Severity=="Warning");
    }
    [Theory]
    [InlineData("arduino-uno",BoardKind.Uno,"A4","A5")]
    [InlineData("esp32-devkit",BoardKind.Esp32,"GPIO21","GPIO22")]
    public void GeneratedI2cInitialisationCountsImplicitAndExplicitBusPins(string boardId,BoardKind kind,string sda,string scl)
    {
        using var e=new ElectricalTestCircuit();var board=e.Add(boardId);var lcd=e.Add("lcd1602-i2c");e.Wire(board,sda,lcd,"SDA");e.Wire(board,scl,lcd,"SCL");
        var generated=new ArduinoCodeGenerator(new JsonCodeTemplateRepository()).Generate(e.Project.Model,e.Nets(),new(Board:kind));
        Assert.True(generated.Success);Assert.Empty(Analyze(e,generated.Text,kind));
    }
    [Fact]
    public void PrefixedRawStringsAndContinuedCommentsAreIgnored()
    {
        using var e=new ElectricalTestCircuit();e.Add("esp32-devkit");
        var text="auto s=u8R\"tag(\" digitalWrite(34,HIGH); \" )tag\";\n// continued \\\n digitalWrite(35,HIGH);\n#define MACRO(x) \\\n digitalWrite(39,HIGH);\n";
        Assert.Empty(Analyze(e,text,BoardKind.Esp32));
    }
    [Fact]
    public void ParenthesizedConstantsAreRecognizedButUnknownObjectMethodsAreNotAssumedToBeGpio()
    {
        using var e=new ElectricalTestCircuit();
        Assert.Contains(Analyze(e,"#define P (GPIO34)\npinMode((P), OUTPUT);",BoardKind.Esp32),p=>p.Message.Contains("только ввод"));
        Assert.DoesNotContain(Analyze(e,"object.attach(34); object.digitalWrite(35,HIGH);",BoardKind.Esp32),p=>p.Message.Contains("только ввод"));
    }
}
