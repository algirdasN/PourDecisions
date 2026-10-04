using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PourDecisions.Core.Entities;

namespace PourDecisions.Desktop.ViewModels;

public partial class ModifyIngredientTypeViewModel(IngredientType ingredientType) : ViewModelBase
{
    public static readonly Comparer<ModifyIngredientTypeViewModel> NameComparer =
        Comparer<ModifyIngredientTypeViewModel>.Create((x, y) =>
            string.Compare(x.Name, y.Name, StringComparison.Ordinal));

    public Func<ModifyIngredientTypeViewModel, bool, Task<bool>>? RequestTrackedChange;
    private bool _isUpdating;
    public int Id { get; } = ingredientType.Id;

    [ObservableProperty]
    public partial string Name { get; set; } = ingredientType.Name;

    [ObservableProperty]
    public partial bool IsTracked { get; set; } = ingredientType.IsTracked;

    public event Action<ModifyIngredientTypeViewModel>? RenameButtonClicked;
    public event Action<ModifyIngredientTypeViewModel>? DeleteButtonClicked;

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

    partial void OnIsTrackedChanged(bool oldValue, bool newValue)
    {
        if (_isUpdating)
        {
            return;
        }

        _ = RequestTrackedChangeAsync(oldValue, newValue);
    }

    private async Task RequestTrackedChangeAsync(bool oldValue, bool newValue)
    {
        if (RequestTrackedChange is null)
        {
            return;
        }

        if (!await RequestTrackedChange.Invoke(this, newValue))
        {
            _isUpdating = true;
            IsTracked = oldValue;
            _isUpdating = false;
        }
    }
}
