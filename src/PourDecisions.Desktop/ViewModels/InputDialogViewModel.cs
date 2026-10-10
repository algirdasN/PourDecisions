using CommunityToolkit.Mvvm.ComponentModel;

namespace PourDecisions.Desktop.ViewModels;

public partial class InputDialogViewModel(string message, string initialValue = "") : ViewModelBase
{
    public string Message { get; } = message;

    [ObservableProperty]
    public partial string Value { get; set; } = initialValue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => ErrorMessage is not null;

    partial void OnValueChanged(string value)
    {
        ErrorMessage = null;
    }
}
