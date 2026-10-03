using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PourDecisions.Desktop.ViewModels;

public partial class InputDialogViewModel(
    string message,
    string initialValue = "",
    Func<string, ValidationResult?>? validator = null)
    : ViewModelBase
{
    private readonly Func<string, ValidationResult?>? _validator = validator;

    public string Message { get; } = message;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(InputDialogViewModel), nameof(Validate))]
    public partial string Value { get; set; } = initialValue;

    public bool HasValidationError()
    {
        ValidateAllProperties();
        return HasErrors;
    }

    public static ValidationResult? Validate(string value, ValidationContext context)
    {
        var instance = (InputDialogViewModel)context.ObjectInstance;

        return instance._validator is not null
            ? instance._validator(value)
            : ValidationResult.Success;
    }
}
