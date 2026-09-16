using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PourDecisions.Core.Entities;
using PourDecisions.Core.Enums;
using PourDecisions.Shared.Extensions;

namespace PourDecisions.Desktop.ViewModels;

public partial class IngredientTypeViewModel : ViewModelBase
{
    public static readonly Comparer<IngredientTypeViewModel> NameComparer =
        Comparer<IngredientTypeViewModel>.Create((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

    public readonly int Id;
    public readonly string Name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    private ObservableCollection<BottleViewModel> _bottles = [];

    [ObservableProperty]
    private bool _isExpanded;

    public IngredientTypeViewModel(IngredientType type, ICollection<Bottle> bottles)
    {
        Id = type.Id;
        Name = type.Name;

        LoadBottles(bottles);
    }

    public string DisplayName => $"{Name} ({Bottles.Count})";

    public event Action<string>? AddFormClicked;
    public event Action<int, FillLevel>? FillLevelChanged;
    public event Action<int, string, int, string>? DeleteBottleClicked;

    public void LoadBottles(ICollection<Bottle> bottles)
    {
        foreach (var vm in Bottles)
        {
            vm.FillLevelChanged -= OnFillLevelChanged;
            vm.DeleteBottleClicked -= OnDeleteBottleClicked;
        }

        Bottles = bottles
            .OrderBy(bottle => bottle.Name)
            .Select(bottle =>
            {
                var vm = new BottleViewModel(bottle);
                vm.FillLevelChanged += OnFillLevelChanged;
                vm.DeleteBottleClicked += OnDeleteBottleClicked;
                return vm;
            })
            .ToObservableCollection();
    }

    [RelayCommand]
    private void AddForm()
    {
        IsExpanded = true;
        AddFormClicked?.Invoke(Name);
    }

    private void OnFillLevelChanged(int bottleId, FillLevel newFill)
    {
        FillLevelChanged?.Invoke(bottleId, newFill);
    }

    private void OnDeleteBottleClicked(int bottleId, string bottleName)
    {
        DeleteBottleClicked?.Invoke(bottleId, bottleName, Id, Name);
    }
}
