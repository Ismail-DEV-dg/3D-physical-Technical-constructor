using CommunityToolkit.Mvvm.Input;
namespace CircuitEditor.App.ViewModels;
public sealed class ChoiceDialogViewModel(string title, string message, IReadOnlyList<string> choices, Action<string> choose)
{
    public string Title => title;
    public string Message => message;
    public IReadOnlyList<string> Choices => choices;
    public IRelayCommand<string> ChooseCommand { get; } = new RelayCommand<string>(s => { if (s is not null) choose(s); });
}
