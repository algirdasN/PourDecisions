using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PourDecisions.Core.Entities;

namespace PourDecisions.Desktop.ViewModels;

public partial class ModifyIngredientTypeViewModel(IngredientType ingredientType) : ViewModelBase
{
    public int Id { get; } = ingredientType.Id;

    public string Name { get; } = ingredientType.Name;

    [ObservableProperty]
    public partial bool IsTracked { get; set; } = ingredientType.IsTracked;

    public event Action<int>? RenameButtonClicked;
    public event Action<int>? DeleteButtonClicked;

    [RelayCommand]
    private void ToggleTracked()
    {
    }

    [RelayCommand]
    private void Rename()
    {
        RenameButtonClicked?.Invoke(Id);
    }

    [RelayCommand]
    private void Delete()
    {
        DeleteButtonClicked?.Invoke(Id);
    }
}
