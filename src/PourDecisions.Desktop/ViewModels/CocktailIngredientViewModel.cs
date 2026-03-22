using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PourDecisions.Application.Models;
using PourDecisions.Core.Entities;
using PourDecisions.Core.Enums;
using PourDecisions.Desktop.Models;

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

    public event Action? OnChanged;
    public event Action<CocktailIngredientViewModel>? OnNavigationIconClicked;
    public event Action<CocktailIngredientViewModel>? OnDeleteClicked;

    public CocktailIngredientSummary GetIngredientData()
    {
        return new CocktailIngredientSummary(int.Parse(AmountText), Unit, Name);
    }

    public new void ValidateAllProperties()
    {
        base.ValidateAllProperties();
    }

    [RelayCommand]
    private void Navigate()
    {
        OnNavigationIconClicked?.Invoke(this);
    }

    [RelayCommand]
    private void Delete()
    {
        OnDeleteClicked?.Invoke(this);
        OnChanged?.Invoke();
        WeakReferenceMessenger.Default.Send(new DuplicateNameCheckMessage());
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

        WeakReferenceMessenger.Default.Send(new IngredientValidationChangedMessage(sender, amountError, nameError));
    }

    partial void OnAmountTextChanged(string value)
    {
        OnChanged?.Invoke();
    }

    partial void OnUnitChanged(AmountUnit value)
    {
        OnChanged?.Invoke();
    }

    partial void OnNameChanged(string value)
    {
        OnChanged?.Invoke();
        WeakReferenceMessenger.Default.Send(new DuplicateNameCheckMessage());
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
