using CommunityToolkit.Mvvm.ComponentModel;

namespace PourDecisions.Desktop.ViewModels;

public partial class IngredientFilterViewModel(string name) : ViewModelBase
{
    [ObservableProperty]
    private bool _isSelected;

    public string Name { get; } = name;

    public event Action? SelectionChanged;

    partial void OnIsSelectedChanged(bool value)
    {
        SelectionChanged?.Invoke();
    }
}
