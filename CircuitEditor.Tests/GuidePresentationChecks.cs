using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CircuitEditor.App.Services;
using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Views;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

internal static class GuidePresentationChecks
{
    internal static void Run()
    {
        var definitions = new JsonComponentLibraryRepository();
        var library = new JsonGuideRepository(definitions).Load().Value!;
        var model = new ErrorHelpViewModel(new ErrorDocumentationService(new JsonErrorHelpRepository()),
            new TestDiagnosticServices(), library);
        var view = new ErrorHelpWindow { DataContext = model, Left = -10000, Top = -10000, ShowInTaskbar = false };
        try
        {
            view.Show();
            view.UpdateLayout();
            Assert.Contains(Controls<Button>(view), button => button.IsVisible && Equals(button.CommandParameter, "lessons"));
            var nano = Controls<Button>(view).Single(button => Equals(button.Content, "Nano") && button.Command == model.OpenSectionCommand);
            nano.Command.Execute(nano.CommandParameter);
            view.UpdateLayout();
            var browser = Assert.IsType<GuideBrowserView>(view.FindName("GuideBrowser"));
            Assert.True(browser.IsVisible);
            Assert.Equal(29, Controls<ListBox>(browser).Single().Items.Count);
            Assert.Contains(Controls<TextBox>(browser), field => field.IsVisible && field.Text.Contains("void setup()"));

            var lessons = Controls<Button>(view).First(button => Equals(button.Content, "Уроки с кодом"));
            lessons.Command.Execute(lessons.CommandParameter);
            view.UpdateLayout();
            Assert.Equal("uno-blink", model.Guide.Chapter?.Id);
            var next = Controls<Button>(browser).Single(button => button.Command == model.Guide.NextCommand);
            next.Command.Execute(null);
            Assert.Equal("uno-button", model.Guide.Chapter?.Id);
            var previous = Controls<Button>(browser).Single(button => button.Command == model.Guide.PreviousCommand);
            previous.Command.Execute(null);
            Assert.Equal("uno-blink", model.Guide.Chapter?.Id);

            model.BackCommand.Execute(null);
            view.UpdateLayout();
            Assert.False(browser.IsVisible);
            model.SelectCode("LED_SHORT");
            view.UpdateLayout();
            var related = Controls<Button>(view).Single(button => (string?)button.CommandParameter == "esp32" &&
                button.Command == model.OpenRelatedCommand);
            Assert.True(related.IsVisible);
            related.Command.Execute(related.CommandParameter);
            view.UpdateLayout();
            Assert.True(browser.IsVisible);
            Assert.Equal("led", model.Guide.Chapter?.Id);
        }
        finally { view.Close(); }
    }

    private static IEnumerable<T> Controls<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed) yield return typed;
            foreach (var descendant in Controls<T>(child)) yield return descendant;
        }
    }
}
