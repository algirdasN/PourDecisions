using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PourDecisions.Core.Entities;

namespace PourDecisions.Desktop.ViewModels;

public partial class ModifyIngredientTypeViewModel(IngredientType ingredientType) : ViewModelBase
{
    public static readonly Comparer<ModifyIngredientTypeViewModel> NameComparer =
        Comparer<ModifyIngredientTypeViewModel>.Create((x, y) =>
            string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));

    public int Id { get; } = ingredientType.Id;

    [ObservableProperty]
    public partial string Name { get; set; } = ingredientType.Name;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsTracked { get; set; } = ingredientType.IsTracked;

    public event Action<ModifyIngredientTypeViewModel, bool>? ToggleTrackedButtonClicked;
    public event Action<ModifyIngredientTypeViewModel>? RenameButtonClicked;
    public event Action<ModifyIngredientTypeViewModel>? DeleteButtonClicked;

    [RelayCommand]
    private void ToggleTracked()
    {
        if (IsBusy)
        {
            return;
        }

        ToggleTrackedButtonClicked?.Invoke(this, !IsTracked);
    }

    [RelayCommand]
    private void Rename()
    {
        RenameButtonClicked?.Invoke(this);
    }

    [RelayCommand]
    private void Delete()
    {
        DeleteButtonClicked?.Invoke(this);
    }
}
