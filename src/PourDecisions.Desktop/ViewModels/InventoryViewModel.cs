using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using PourDecisions.Application.Services;
using PourDecisions.Core.Enums;
using PourDecisions.Desktop.Services;
using PourDecisions.Shared.Extensions;

namespace PourDecisions.Desktop.ViewModels;

public partial class InventoryViewModel(
    IBottleService bottleService,
    IDialogService dialogService,
    IIngredientService ingredientService)
    : ViewModelBase, IAsyncLoadable
{
    [ObservableProperty]
    private AddBottleViewModel? _addBottleForm;

    private List<string> _ingredientTypeNames = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private ObservableCollection<IngredientTypeViewModel> _ingredientTypes = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ListColumn))]
    private bool _isAddingBottle;

    public bool IsEmpty => IngredientTypes.Count == 0;
    public int ListColumn => IsAddingBottle ? 0 : 1;

    public async Task LoadAsync()
    {
        var bottleTask = bottleService.GetBottlesWithTypeAsync();
        var ingredientTypeTask = ingredientService.GetTrackedIngredientTypeNamesAsync();

        await Task.WhenAll(bottleTask, ingredientTypeTask);

        _ingredientTypeNames = ingredientTypeTask.Result;

        var typeViewModels = bottleTask.Result
            .GroupBy(bottle => bottle.Type)
            .OrderBy(group => group.Key.Name)
            .Select(group =>
            {
                var vm = new IngredientTypeViewModel(group.Key, group.ToList());
                vm.AddFormClicked += OnAddFormClicked;
                vm.FillLevelChanged += OnFillLevelChanged;
                vm.DeleteBottleClicked += OnDeleteButtonClicked;
                return vm;
            })
            .ToList();

        IngredientTypes = new ObservableCollection<IngredientTypeViewModel>(typeViewModels);
    }

    [RelayCommand]
    private void AddForm()
    {
        OnAddFormClicked(string.Empty);
    }

    private void OnAddFormClicked(string typeName)
    {
        AddBottleForm?.OnAddButtonClicked -= OnAddBottleClicked;
        AddBottleForm?.OnCancelButtonClicked -= OnAddFormCancelled;

        IsAddingBottle = true;
        AddBottleForm = new AddBottleViewModel(_ingredientTypeNames, typeName);
        AddBottleForm.OnAddButtonClicked += OnAddBottleClicked;
        AddBottleForm.OnCancelButtonClicked += OnAddFormCancelled;
    }

    private async void OnAddBottleClicked(string typeName, string bottleName, int volume, FillLevel fillLevel)
    {
        var bottle = await bottleService.AddBottleAsync(typeName, bottleName, volume, fillLevel);
        var ingredientType = bottle.Type;

        var allBottles = await bottleService.GetBottlesOfTypeAsync(ingredientType.Id);
        var ingredientViewModel = IngredientTypes.FirstOrDefault(type => type.Id == ingredientType.Id);

        if (ingredientViewModel is null)
        {
            ingredientViewModel = new IngredientTypeViewModel(ingredientType, allBottles);
            ingredientViewModel.AddFormClicked += OnAddFormClicked;
            ingredientViewModel.FillLevelChanged += OnFillLevelChanged;
            ingredientViewModel.DeleteBottleClicked += OnDeleteButtonClicked;

            IngredientTypes.InsertIntoSorted(ingredientViewModel, IngredientTypeViewModel.NameComparer);
        }
        else
        {
            ingredientViewModel.LoadBottles(allBottles);
        }

        ingredientViewModel.IsExpanded = true;

        if (!_ingredientTypeNames.Contains(ingredientType.Name))
        {
            _ingredientTypeNames.InsertIntoSorted(ingredientType.Name);
        }

        AddBottleForm?.IngredientTypeNames = new ObservableCollection<string>(_ingredientTypeNames);
    }

    private void OnAddFormCancelled()
    {
        AddBottleForm?.OnAddButtonClicked -= OnAddBottleClicked;
        AddBottleForm?.OnCancelButtonClicked -= OnAddFormCancelled;
        AddBottleForm = null;
        IsAddingBottle = false;
    }

    private async void OnFillLevelChanged(int bottleId, FillLevel newFill)
    {
        await bottleService.UpdateBottleFillLevelAsync(bottleId, newFill);
    }

    private async void OnDeleteButtonClicked(int bottleId, string bottleName, int typeId, string typeName)
    {
        var message = $"Are you sure you want to delete '{bottleName}' ({typeName})?";

        var result = await dialogService.ShowConfirmationDialogAsync("Delete bottle", message, "Delete", "Cancel");

        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        await bottleService.DeleteBottleAsync(bottleId);
        var bottles = await bottleService.GetBottlesOfTypeAsync(typeId);
        var ingredientViewModel = IngredientTypes.First(type => type.Id == typeId);

        if (bottles.Count == 0)
        {
            IngredientTypes.Remove(ingredientViewModel);
        }
        else
        {
            ingredientViewModel.LoadBottles(bottles);
        }
    }

    private void OnIngredientTypesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsEmpty));
    }

    partial void OnIngredientTypesChanged(ObservableCollection<IngredientTypeViewModel>? oldValue,
        ObservableCollection<IngredientTypeViewModel> newValue)
    {
        if (oldValue != null)
        {
            oldValue.CollectionChanged -= OnIngredientTypesCollectionChanged;
        }

        newValue.CollectionChanged += OnIngredientTypesCollectionChanged;
    }
}
