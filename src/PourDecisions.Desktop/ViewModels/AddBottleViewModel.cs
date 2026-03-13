using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PourDecisions.Core.Enums;

namespace PourDecisions.Desktop.ViewModels;

public partial class AddBottleViewModel(IEnumerable<string> ingredientTypeNames, string typeName) : ViewModelBase
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Bottle name is required")]
    private string _bottleName = string.Empty;

    [ObservableProperty]
    private FillLevel _fillLevel = FillLevel.Full;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Type name is required")]
    [MinLength(3, ErrorMessage = "Type name must be at least 3 characters")]
    private string _ingredientTypeName = typeName;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(AddBottleViewModel), nameof(ValidateVolume))]
    private string _volumeText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _ingredientTypeNames = new(ingredientTypeNames);

    public FillLevel[] FillLevels { get; } = Enum.GetValues<FillLevel>();

    public event Action<string, string, int, FillLevel>? OnAddButtonClicked;
    public event Action? OnCancelButtonClicked;

    [RelayCommand]
    private void AddBottle()
    {
        ValidateAllProperties();
        if (HasErrors)
        {
            return;
        }

        OnAddButtonClicked?.Invoke(IngredientTypeName, BottleName, int.Parse(VolumeText), FillLevel);
        IngredientTypeName = string.Empty;
        BottleName = string.Empty;
        VolumeText = string.Empty;
        FillLevel = FillLevel.Full;
    }

    [RelayCommand]
    private void Cancel()
    {
        OnCancelButtonClicked?.Invoke();
    }

    public static ValidationResult? ValidateVolume(string value, ValidationContext context)
    {
        if (!int.TryParse(value, out var volume) || volume <= 0)
        {
            return new ValidationResult("Volume must be a positive number");
        }

        return ValidationResult.Success;
    }
}
