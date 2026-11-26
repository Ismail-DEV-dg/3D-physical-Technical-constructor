using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Code;
using CircuitEditor.Core.Layout;
using CircuitEditor.Core.Validation;
using CircuitEditor.Infrastructure.Repositories;
namespace CircuitEditor.Tests;
public sealed class DiagnosticsAndPreparationTests
{
    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void PrepareManualSketchPreservesInstructionsAndSupportsGeneration(string newline)
    {
        var manual=$"// my sketch{newline}void setup(){{{newline} pinMode(13,OUTPUT);{newline}}}{newline}void loop(){{digitalWrite(13,HIGH);}}{newline}";
        var prepared=AutoRegionInitializer.Prepare(manual);Assert.True(prepared.Success,prepared.Message);
        Assert.Contains("pinMode(13,OUTPUT);",prepared.Text);Assert.Contains("digitalWrite(13,HIGH);",prepared.Text);
        Assert.Equal(3,prepared.Text.Split("// <auto>").Length-1);
        using var e=new ElectricalTestCircuit();e.Add("arduino-uno");
        var generated=new ArduinoCodeGenerator(new JsonCodeTemplateRepository()).Generate(e.Project.Model,e.Nets(),new(prepared.Text));
        Assert.True(generated.Success);Assert.Contains("digitalWrite(13,HIGH);",generated.Text);
        Assert.False(AutoRegionInitializer.Prepare(prepared.Text).Success);
    }
    [Theory]
    [InlineData("void setup(int x) {} void loop() {}")]
    [InlineData("void setup() {} void setup() {} void loop() {}")]
    [InlineData("// <auto>\nvoid setup() {} void loop() {}")]
    [InlineData("void setup(){ void loop() {} }")]
    public void AmbiguousOrBrokenSketchIsNotModified(string text)
    {var result=AutoRegionInitializer.Prepare(text);Assert.False(result.Success);Assert.Equal(text,result.Text);}
    [Fact]
    public void HelpSearchCopyAndDirectArticleLinksWork()
    {
        var services=new TestDiagnosticServices();var documentation=new ErrorDocumentationService(new JsonErrorHelpRepository());
        Assert.True(new JsonErrorHelpRepository().Load().Success);
        var vm=new ErrorHelpViewModel(documentation,services);vm.SelectCode("CODE_AUTO_MARKERS");
        Assert.Contains("старых проектах",vm.ArticleText);vm.CopyCommand.Execute(null);Assert.Contains("<auto>",services.Copied!);
        vm.Search="[Код] [Error] [CODE_AUTO_MARKERS] Произвольный текст скопированного сообщения";Assert.Single(vm.Articles);
        vm.Search="маркеры аута";Assert.Single(vm.Articles);Assert.Equal("CODE_AUTO_MARKERS",vm.Selected!.Code);
        vm.Search="A15";Assert.Single(vm.Articles);Assert.Equal("LED_SHORT",vm.Selected!.Code);
        vm.Search="no matching article";Assert.Empty(vm.Articles);Assert.Contains("ничего не найдено",vm.ArticleText);
        vm.SelectCode("CODE_INPUT_ONLY");Assert.Equal("CODE_INPUT_ONLY",vm.Selected!.Code);
        services.Busy=true;vm.CopyCommand.Execute(null);Assert.Contains("занят",vm.Feedback);
    }
    [Fact]
    public void DialogAndUnifiedIssuesCanBeCopiedAndOpenMatchingDocumentation()
    {
        var services=new TestDiagnosticServices();var documentation=new ErrorDocumentationService(new JsonErrorHelpRepository());
        var message="Маркеры <auto> отсутствуют.";
        var dialog=new ChoiceDialogViewModel("Предупреждение",message,["ОК"],_=>{},services,services,documentation);
        dialog.CopyCommand.Execute(null);Assert.Equal(message,services.Copied);dialog.HelpCommand.Execute(null);Assert.Equal("CODE_AUTO_MARKERS",services.OpenedCode);
        using var e=new ElectricalTestCircuit();using var code=new CodeViewModel(e.Project);
        code.ProblemsPanel.SetAnalysis([new(message,Code:"CODE_AUTO_MARKERS")]);
        using var issues=new DiagnosticsViewModel(null,code,services,documentation,services);
        var row=Assert.Single(issues.Items);Assert.Contains("функций setup() или loop()",row.Article.Fix);
        row.CopyCommand.Execute(null);Assert.Contains("CODE_AUTO_MARKERS",services.Copied!);
        row.HelpCommand.Execute(null);Assert.Equal("CODE_AUTO_MARKERS",services.OpenedCode);
        issues.Search="auto";issues.Source="Код";issues.Severity="Предупреждения";Assert.Single(issues.Items);
        issues.CopyAllCommand.Execute(null);Assert.Contains(message,services.Copied!);
        issues.Search="not matching";Assert.True(issues.IsEmpty);
    }
    [Fact]
    public void SameBreadboardRowShortsLedAndErrorIsIndependentOfMissingMarkers()
    {
        using var e=new ElectricalTestCircuit();var board=e.Add("arduino-uno");var led=e.Add("led");
        var breadboard=e.Environment.Mockup.Board!;var hole=breadboard.Definition.Holes.Single(h=>h.Id=="left/A/15");
        Assert.True(e.Environment.Mockup.TryPlace(led,new(breadboard.X+hole.Position.X,breadboard.Y+hole.Position.Y)));
        Assert.Equal(e.Nets().Find(led.Model.Id,"A")!.Id,e.Nets().Find(led.Model.Id,"K")!.Id);
        Assert.Single(e.Check(new LedShortRule()));
        var manual=new SketchDocument("void setup(){pinMode(13,OUTPUT);}\nvoid loop(){digitalWrite(13,HIGH);}");
        var generator=new ArduinoCodeGenerator(new JsonCodeTemplateRepository());
        var failed=generator.Generate(e.Project.Model,e.Nets(),manual);Assert.Contains(failed.Problems,p=>p.Code=="CODE_AUTO_MARKERS");
        var prepared=AutoRegionInitializer.Prepare(manual.Text);Assert.True(prepared.Success);
        var result=generator.Generate(e.Project.Model,e.Nets(),manual with {Text=prepared.Text});Assert.True(result.Success);
        Assert.Contains(result.Problems,p=>p.Code=="CODE_PIN_CONNECTION");
    }
    [Fact]
    public void LedInDifferentNetsDoesNotGetShortWarning()
    {
        using var e=new ElectricalTestCircuit();var board=e.Add("arduino-uno");var led=e.Add("led");e.Wire(board,"D9",led,"A");e.Wire(board,"GND",led,"K");
        Assert.Empty(e.Check(new LedShortRule()));
    }
    [Fact]
    public async Task AnalysisNeverLogsProblemCountsAndOutputDeduplicatesErrors()
    {
        using var e=new ElectricalTestCircuit();e.Add("arduino-uno");
        var worker=new CodeAnalysisService(new ArduinoCodeAnalyzer(new JsonCodeCompletionRepository(),new JsonCodeTemplateRepository()),new Core.Netlist.NetlistBuilder(new JsonElectricalLibraryRepository()));
        using var code=new CodeViewModel(e.Project,analysisService:worker);
        code.Text="// nothing";await code.AnalysisTask;code.Text="digitalWrite(8,HIGH);";await code.AnalysisTask;
        Assert.NotEmpty(code.ProblemsPanel.Items);Assert.Empty(code.Output.Lines);
        code.Output.Append("operation failed");code.Output.Append("operation failed");Assert.Single(code.Output.Lines);
        code.Output.ClearCommand.Execute(null);code.Output.Append("operation failed");Assert.Single(code.Output.Lines);
    }
}
