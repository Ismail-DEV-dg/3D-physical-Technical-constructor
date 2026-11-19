using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CircuitEditor.App.ViewModels;
using CircuitEditor.App.Views;
using CircuitEditor.Core.Chips;
using CircuitEditor.Infrastructure.Repositories;

namespace CircuitEditor.Tests;

internal static class ChipPresentationChecks
{
    public static void Run(ResourceDictionary resources)
    {
        var model = new ChipViewModel(new JsonLogicFamilyRepository());
        var view = new ChipView { DataContext = model, Resources = resources };
        Render(view);
        model.ShowGraphCommand.Execute(null);
        model.Graph.GateKind = ChipGateKind.Nand;
        model.Graph.GatePorts = "P1, P2";
        model.Graph.AddGateCommand.Execute(null);
        Render(view);
        Assert.Equal(8, model.Pins.Count);
    }

    private static void Render(FrameworkElement view)
    {
        const int width = 780;
        const int height = 900;
        view.Measure(new Size(width, height));
        view.Arrange(new Rect(0, 0, width, height));
        view.UpdateLayout();
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(view);
    }
}
