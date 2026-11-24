using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CircuitEditor.App.Services;
using CircuitEditor.App.Views;
using CircuitEditor.App.ViewModels;

namespace CircuitEditor.Tests;

internal static class DialogLayoutChecks
{
    public static void Run(ResourceDictionary resources)
    {
        Application.Current.Resources.MergedDictionaries.Add(resources);
        var theme = new ThemeService(new WpfThemeResources(resources));
        try
        {
            foreach (var kind in new[] { ThemeKind.NeonDark, ThemeKind.LightTech })
            {
                Assert.True(theme.SetTheme(kind), theme.ErrorMessage);
                foreach (var width in new double[] { 440, 500, 620 })
                    CheckDialog(width);
            }
        }
        finally
        {
            theme.SetTheme(ThemeKind.NeonDark);
            Application.Current.Resources.MergedDictionaries.Remove(resources);
        }
    }

    private static void CheckDialog(double width)
    {
        const string save = "Сохранить копию проекта и закрыть";
        const string resume = "Продолжить";
        string? selected = null;
        bool dismissed = false;
        var model = new ChoiceDialogViewModel("Произошла ошибка", string.Join(" ", Enumerable.Repeat("Длинное сообщение.", 20)),
            [resume, save], action => selected = action, dismiss: () => dismissed = true);
        var dialog = new ChoiceDialog { Width = width, SizeToContent = SizeToContent.Height, DataContext = model, ShowActivated = false };
        try
        {
            dialog.Show();
            dialog.UpdateLayout();
            var buttons = FindButtons(dialog).ToList();
            var confirm = Assert.Single(buttons, button => Equals(button.Content, resume));
            var saveButton = Assert.Single(buttons, button => Equals(button.Content, save));
            Assert.True(confirm.IsDefault);
            Assert.False(saveButton.IsDefault);
            Assert.True(saveButton.ActualWidth > 0);
            var position = saveButton.TranslatePoint(new Point(0, 0), dialog);
            Assert.InRange(position.X + saveButton.ActualWidth, 0, width);
            confirm.Command.Execute(confirm.CommandParameter);
            Assert.Equal(resume, selected);
            model.DismissCommand.Execute(null);
            Assert.True(dismissed);
        }
        finally { dialog.Close(); }
    }

    private static IEnumerable<Button> FindButtons(DependencyObject node)
    {
        if (node is Button button) yield return button;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            foreach (var child in FindButtons(VisualTreeHelper.GetChild(node, i)))
                yield return child;
    }
}
