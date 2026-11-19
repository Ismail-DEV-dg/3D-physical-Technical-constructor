using CircuitEditor.Core.Chips;

namespace CircuitEditor.App.Services;

public interface IChipPinoutRenderer
{
    byte[] Render(ChipDefinition definition);
}
