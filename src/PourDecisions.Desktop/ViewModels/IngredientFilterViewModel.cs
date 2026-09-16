using CommunityToolkit.Mvvm.ComponentModel;

namespace PourDecisions.Desktop.ViewModels;

public partial class IngredientFilterViewModel(int id, string name) : ViewModelBase
{
    [ObservableProperty]
    private bool _isSelected;

    public int Id { get; set; } = id;
    public string Name { get; } = name;

    public event Action? SelectionChanged;

    partial void OnIsSelectedChanged(bool value)
    {
        SelectionChanged?.Invoke();
    }
}
