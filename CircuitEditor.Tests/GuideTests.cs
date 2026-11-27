using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

public sealed class GuideTests
{
    [Fact]
    public void EveryBoardIncludesExactlyOneChapterPerLibraryComponentAndStandaloneSketches()
    {
        var components = new JsonComponentLibraryRepository();
        var catalog = new JsonGuideRepository(components).Load();
        Assert.True(catalog.Success, catalog.Error);
        var library = catalog.Value!;
        var ids = components.Load().Components.Select(c => c.Id).Order().ToArray();
        Assert.Equal(28, ids.Length);

        foreach (var board in new[] { "uno", "nano", "esp32" })
        {
            var chapters = library.Chapters(board);
            Assert.Equal(ids, chapters.Where(c => c.Id != "start").Select(c => c.Id).Order().ToArray());
            foreach (var chapter in chapters)
            {
                Assert.Contains("Как подключить", chapter.Instructions);
                Assert.Contains("Как проверить", chapter.Instructions);
                Assert.Contains("Если не работает", chapter.Instructions);
                Assert.DoesNotContain("{{", chapter.Instructions);
                Assert.NotNull(chapter.Code);
                Assert.Contains("void setup()", chapter.Code);
                Assert.Contains("void loop()", chapter.Code);
                Assert.DoesNotContain("{{", chapter.Code);
            }
        }

        var esp32 = library.Chapters("esp32");
        Assert.Contains("GPIO26", esp32.Single(c => c.Id == "hc-sr04").Instructions);
        Assert.Contains("ESP32Servo.h", esp32.Single(c => c.Id == "servo").Code);
        Assert.Contains("Servo.h", library.Chapters("nano").Single(c => c.Id == "servo").Code);
        Assert.Contains("Wire.begin(21, 22)", esp32.Single(c => c.Id == "oled-ssd1306").Code);
        Assert.True(library.Chapters("application").Count >= 8);
    }

    [Fact]
    public void ChipGuideIsSearchableAndDiagnosticsOpenTheRelevantChapter()
    {
        var errors = new JsonErrorHelpRepository();
        var catalog = new JsonGuideRepository(new JsonComponentLibraryRepository()).Load().Value!;
        var help = new ErrorHelpViewModel(new ErrorDocumentationService(errors), new TestDiagnosticServices(), catalog);

        help.OpenSection("application");
        help.Guide.Search = "74HC595 защёлка";
        Assert.Equal("chip-sequential", Assert.Single(help.Guide.Chapters).Id);
        help.Guide.Search = "Excel .chip";
        Assert.Equal("chip-files", Assert.Single(help.Guide.Chapters).Id);

        foreach (var (code, chapter) in new[]
        {
            ("CHIP_UNPOWERED", "chip-nand"), ("CHIP_POWER", "chip-nand"),
            ("CHIP_OUTPUT_CURRENT", "chip-nand"), ("CHIP_OSCILLATION", "chip-editor")
        })
        {
            help.SelectCode(code);
            help.OpenRelatedCommand.Execute("application");
            Assert.Equal(chapter, help.Guide.Chapter?.Id);
            help.BackCommand.Execute(null);
            Assert.Equal(code, help.Selected?.Code);
        }
    }


    [Fact]
    public void ErrorOpensRelatedBoardChapterAndBackReturnsToTheSameError()
    {
        var errors = new JsonErrorHelpRepository();
        var catalog = new JsonGuideRepository(new JsonComponentLibraryRepository()).Load().Value!;
        var clipboard = new TestDiagnosticServices();
        var help = new ErrorHelpViewModel(new ErrorDocumentationService(errors), clipboard, catalog);

        Assert.True(help.IsHome);
        help.SelectCode("LED_SHORT");
        Assert.True(help.IsErrors);
        Assert.True(help.HasRelatedComponent);
        help.OpenRelatedCommand.Execute("esp32");
        Assert.True(help.IsGuide);
        Assert.Equal("led", help.Guide.Chapter?.Id);
        help.Guide.CopyCodeCommand.Execute(null);
        Assert.Contains("const int pin = 25;", clipboard.Copied);

        help.BackCommand.Execute(null);
        Assert.Equal("LED_SHORT", help.Selected?.Code);
        help.BackCommand.Execute(null);
        Assert.True(help.IsHome);
        help.OpenSection("nano");
        help.Guide.Search = "потенциометр показание ручки";
        Assert.Equal("potentiometer", Assert.Single(help.Guide.Chapters).Id);
        Assert.Equal("potentiometer", help.Guide.Chapter?.Id);

        foreach (var article in errors.Load().Value!)
        {
            if (article.ComponentGuideId is { } component)
                Assert.Contains(catalog.Components, chapter => chapter.Id == component);
            if (article.ApplicationGuideId is { } application)
                Assert.Contains(catalog.Application, chapter => chapter.Id == application);
        }
    }
}
