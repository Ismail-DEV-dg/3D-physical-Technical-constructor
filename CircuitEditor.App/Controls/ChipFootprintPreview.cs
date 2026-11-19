using System.Windows;
using System.Windows.Media;
using CircuitEditor.Core.Chips;

namespace CircuitEditor.App.Controls;

public sealed class ChipFootprintPreview : FrameworkElement
{
    public static readonly DependencyProperty DefinitionProperty = DependencyProperty.Register(nameof(Definition),
        typeof(ChipDefinition), typeof(ChipFootprintPreview), new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.AffectsRender));

    public ChipDefinition? Definition
    {
        get => (ChipDefinition?)GetValue(DefinitionProperty);
        set => SetValue(DefinitionProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (Definition is not { } chip || ChipPackage.PinCount(chip.Package) == 0) return;
        var footprint = ChipPackage.Footprint(chip);
        var bounds = footprint.Body;
        var scale = Math.Min(Math.Max(0, ActualWidth - 20) / 12, Math.Max(0, ActualHeight - 16) / (bounds.Height + 2));
        if (scale <= 0) return;

        dc.PushTransform(new TranslateTransform(ActualWidth / 2 - (bounds.X + bounds.Width / 2) * scale,
            ActualHeight / 2 - (bounds.Y + bounds.Height / 2) * scale));
        dc.PushTransform(new ScaleTransform(scale, scale));
        ChipPackageDrawing.Draw(dc, footprint, chip.Name);
        dc.Pop();
        dc.Pop();
    }
}
