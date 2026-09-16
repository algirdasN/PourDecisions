using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PourDecisions.Application.Models;
using PourDecisions.Core.Entities;
using PourDecisions.Core.Enums;

namespace PourDecisions.Desktop.ViewModels;

public partial class CocktailIngredientViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(CocktailIngredientViewModel), nameof(ValidateAmount))]
    private string _amountText;

    [ObservableProperty]
    private bool _hasDuplicateName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NavigationIcon))]
    private bool _isFirst;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Type name is required")]
    [MinLength(3, ErrorMessage = "Type name must be at least 3 characters")]
    [CustomValidation(typeof(CocktailIngredientViewModel), nameof(ValidateUniqueName))]
    private string _name;

    [ObservableProperty]
    private AmountUnit _unit;

    public CocktailIngredientViewModel(CocktailIngredient? cocktailIngredient = null)
    {
        _amountText = cocktailIngredient?.AmountValue.ToString() ?? string.Empty;
        _name = cocktailIngredient?.Type.Name ?? string.Empty;
        _unit = cocktailIngredient?.AmountUnit ?? AmountUnit.Ml;

        ErrorsChanged += OnErrorsChanged;
    }

    public AmountUnit[] AmountUnits { get; } = Enum.GetValues<AmountUnit>();

    public string NavigationIcon => IsFirst ? "﹀" : "︿";

    public event Action? Changed;
    public event Action<CocktailIngredientViewModel, bool>? NavigationIconClicked;
    public event Action<CocktailIngredientViewModel>? DeleteClicked;
    public event Action<object, string?, string?>? ValidationChanged;
    public event Action? DuplicateCheckNeeded;

    public CocktailIngredientSummary GetIngredientData()
    {
        return int.TryParse(AmountText, out var amount)
            ? new CocktailIngredientSummary(amount, Unit, Name)
            : throw new InvalidOperationException("Amount must be a valid integer");
    }

    public void TriggerValidation()
    {
        ValidateAllProperties();
    }

    [RelayCommand]
    private void Navigate()
    {
        Changed?.Invoke();
        NavigationIconClicked?.Invoke(this, IsFirst);
    }

    [RelayCommand]
    private void Delete()
    {
        Changed?.Invoke();
        DeleteClicked?.Invoke(this);
        DuplicateCheckNeeded?.Invoke();
    }

    private void OnErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        if (sender == null)
        {
            return;
        }

        var amountError = GetErrors(nameof(AmountText)).FirstOrDefault() switch
        {
            null => null,
            var result => result.ErrorMessage ?? "Unknown error"
        };

        var nameError = GetErrors(nameof(Name)).FirstOrDefault() switch
        {
            null => null,
            var result => result.ErrorMessage ?? "Unknown error"
        };

        ValidationChanged?.Invoke(sender, amountError, nameError);
    }

    partial void OnAmountTextChanged(string value)
    {
        Changed?.Invoke();
    }

    partial void OnUnitChanged(AmountUnit value)
    {
        Changed?.Invoke();
    }

    partial void OnNameChanged(string value)
    {
        Changed?.Invoke();
        DuplicateCheckNeeded?.Invoke();
    }

    partial void OnHasDuplicateNameChanged(bool value)
    {
        ValidateProperty(Name, nameof(Name));
    }

    public static ValidationResult? ValidateAmount(string value, ValidationContext context)
    {
        if (!int.TryParse(value, out var volume) || volume <= 0)
        {
            return new ValidationResult("Amount must be a positive integer");
        }

        return ValidationResult.Success;
    }

    public static ValidationResult? ValidateUniqueName(string value, ValidationContext context)
    {
        var instance = (CocktailIngredientViewModel)context.ObjectInstance;

        return instance.HasDuplicateName
            ? new ValidationResult("Ingredient names must be unique")
            : ValidationResult.Success;
    }
}
