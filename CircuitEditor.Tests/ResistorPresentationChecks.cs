using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using CircuitEditor.App.Views;
using CircuitEditor.Core.Resistance;

namespace CircuitEditor.Tests;

internal static class ResistorPresentationChecks
{
    public static void Run(ResourceDictionary resources)
    {
        Application.Current.Resources.MergedDictionaries.Add(resources);
        using var project = TestProjectFactory.Create();
        var resistor = project.Add(project.Library.Single(item => item.Id == "resistor"))!;
        var view = new PropertiesView { Resources = resources, DataContext = project };
        var window = new Window { Content = view, Width = 260, Height = 1000,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        window.Show();
        try
        {
            Arrange(view);
            var combos = Descendants(view).OfType<ComboBox>().ToArray();
            var series = combos.Single(combo => AutomationProperties.GetName(combo) == "Ряд номиналов");
            var nominal = combos.Single(combo => AutomationProperties.GetName(combo) == "Сопротивление");
            Assert.True(nominal.IsVisible);
            Assert.DoesNotContain(Descendants(view).OfType<TextBox>(), box =>
                box.IsVisible && AutomationProperties.GetName(box) is "Цвет" or "Номинал");
            series.SelectedItem = ResistorSeriesKind.E96;
            Assert.Equal(ResistorSeriesKind.E96, resistor.ResistorEditor!.SelectedSeries);
            nominal.SelectedItem = resistor.ResistorEditor.Options.Single(option => option.Ohms == 1020m);
            Assert.Equal("1.02 kΩ", resistor.Model.Value);
            Assert.Equal(resistor.ResistorEditor.SelectedOption, nominal.SelectedItem);
            nominal.SelectedItem = resistor.ResistorEditor.Options[^1];
            Arrange(view);
            var custom = Descendants(view).OfType<TextBox>().Single(box =>
                AutomationProperties.GetName(box) == "Своё сопротивление");
            Assert.True(custom.IsVisible);
            custom.Text = "3,33 kΩ";
            custom.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
            resistor.ResistorEditor.ApplyCustomCommand.Execute(null);
            Arrange(view);
            Assert.Equal("3.33 kΩ", resistor.Model.Value);
            Assert.Equal(3330m, resistor.ResistorEditor.SelectedOption!.Ohms);
            Assert.Equal(resistor.ResistorEditor.SelectedOption, nominal.SelectedItem);
            Assert.False(custom.IsVisible);
            series.SelectedItem = ResistorSeriesKind.E12;
            Assert.Equal(3330m, resistor.ResistorEditor.SelectedOption!.Ohms);
            Assert.Equal(resistor.ResistorEditor.SelectedOption, nominal.SelectedItem);

            var led = project.Add(project.Library.Single(item => item.Id == "led"))!;
            Arrange(view);
            Assert.Same(led, project.SelectedComponent);
            Assert.False(nominal.IsVisible);
            Assert.Contains(Descendants(view).OfType<TextBox>(), box =>
                box.IsVisible && AutomationProperties.GetName(box) == "Цвет");
        }
        finally { window.Close(); }
    }

    private static void Arrange(FrameworkElement view)
    {
        view.Measure(new Size(236, 1000));
        view.Arrange(new Rect(0, 0, 236, 1000));
        view.UpdateLayout();
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
