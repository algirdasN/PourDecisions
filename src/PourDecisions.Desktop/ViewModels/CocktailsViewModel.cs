using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PourDecisions.Application.AvailabilityEngine;
using PourDecisions.Application.Services;
using PourDecisions.Desktop.Services;
using PourDecisions.Shared.Extensions;

namespace PourDecisions.Desktop.ViewModels;

public partial class CocktailsViewModel(
    IAvailabilityService availabilityService,
    ICocktailService cocktailService,
    IDialogService dialogService)
    : ViewModelBase, IAsyncLoadable
{
    private List<CocktailSummaryViewModel> _allCocktails = [];

    private List<IngredientFilterViewModel> _allTrackedIngredients = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResults))]
    [NotifyPropertyChangedFor(nameof(ShowClearButton))]
    private ObservableCollection<CocktailSummaryViewModel> _filteredCocktails = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedIngredientsText))]
    private ObservableCollection<IngredientFilterViewModel> _filteredIngredients = [];

    [ObservableProperty]
    private string _ingredientSearchText = string.Empty;

    [ObservableProperty]
    private string _nameSearchText = string.Empty;

    [ObservableProperty]
    private CocktailSummaryViewModel? _selectedCocktail;

    [ObservableProperty]
    private string _selectedIngredientsText = "Select ingredients...";

    [ObservableProperty]
    private bool _showAvailableOnly;


    [ObservableProperty]
    private bool _showFavoriteOnly;

    public bool HasResults => FilteredCocktails.Count > 0;
    public bool ShowClearButton => _allCocktails.Count > 0 && FilteredCocktails.Count == 0;

    public string EmptyStateMessage => _allCocktails.Count == 0
        ? "No cocktails available. Add cocktails in the Edit Cocktails page."
        : "No cocktails match the current filters. Try adjusting the filters or search text.";

    public async Task LoadAsync()
    {
        var cocktailTask = cocktailService.GetAllWithIngredientsAsync();
        var availabilityTask = availabilityService.GetCocktailAvailabilityAsync();

        foreach (var vm in _allCocktails)
        {
            vm.FavoriteToggled -= OnFavoriteToggled;
        }

        foreach (var vm in _allTrackedIngredients)
        {
            vm.SelectionChanged -= OnIngredientSelectionChanged;
        }

        await Task.WhenAll(cocktailTask, availabilityTask);

        var cocktails = cocktailTask.Result;
        var cocktailAvailability = availabilityTask.Result;

        _allCocktails = cocktails
            .Select(cocktail =>
            {
                var vm = new CocktailSummaryViewModel(cocktail, cocktailAvailability[cocktail.Id]);
                vm.FavoriteToggled += OnFavoriteToggled;
                return vm;
            })
            .ToList();

        _allTrackedIngredients = cocktails
            .SelectMany(cocktail => cocktail.CocktailIngredients)
            .Select(ingredient => ingredient.Type)
            .Where(ingredientType => ingredientType.IsTracked)
            .Distinct()
            .OrderBy(ingredientType => ingredientType.Name)
            .Select(ingredientType =>
            {
                var vm = new IngredientFilterViewModel(ingredientType.Id, ingredientType.Name);
                vm.SelectionChanged += OnIngredientSelectionChanged;
                return vm;
            })
            .ToList();

        FilteredCocktails = _allCocktails.ToObservableCollection();

        FilteredIngredients = _allTrackedIngredients.ToObservableCollection();

        SelectedCocktail = FilteredCocktails.FirstOrDefault();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        NameSearchText = string.Empty;
        ShowAvailableOnly = false;
        ShowFavoriteOnly = false;
        ClearIngredientFilters();
    }

    [RelayCommand]
    private void ClearIngredientFilters()
    {
        IngredientSearchText = string.Empty;
        foreach (var ingredient in _allTrackedIngredients)
        {
            ingredient.IsSelected = false;
        }

        OnIngredientSelectionChanged();
    }

    private void OnFavoriteToggled(int cocktailId, bool isFavorite)
    {
        _ = SetFavoriteAsync(cocktailId, isFavorite);
        FilterCocktails();
    }

    private void OnIngredientSelectionChanged()
    {
        var text = string.Join(", ", _allTrackedIngredients.Where(vm => vm.IsSelected).Select(vm => vm.Name));
        SelectedIngredientsText = string.IsNullOrWhiteSpace(text)
            ? "Select ingredients..."
            : text;

        FilterCocktails();
    }

    private async Task SetFavoriteAsync(int cocktailId, bool isFavorite)
    {
        try
        {
            await cocktailService.SetFavoriteAsync(cocktailId, isFavorite);
        }
        catch (Exception e)
        {
            await dialogService.ShowInformationDialogAsync("Failed to update favorite status", e.Message);
        }
    }

    private void FilterCocktails()
    {
        FilteredCocktails = _allCocktails
            .Where(vm =>
                (!ShowAvailableOnly || vm.AvailabilityStatus == AvailabilityStatus.Available)
                && (!ShowFavoriteOnly || vm.IsFavorite)
                && (string.IsNullOrWhiteSpace(NameSearchText)
                    || vm.Name.Contains(NameSearchText, StringComparison.OrdinalIgnoreCase))
                && _allTrackedIngredients
                    .Where(ingredientVm => ingredientVm.IsSelected)
                    .All(ingredientVm => vm.TrackedIngredientIds.Contains(ingredientVm.Id))
            )
            .ToObservableCollection();

        if (SelectedCocktail == null || !FilteredCocktails.Contains(SelectedCocktail))
        {
            SelectedCocktail = FilteredCocktails.FirstOrDefault();
        }
    }

    partial void OnShowAvailableOnlyChanged(bool value)
    {
        FilterCocktails();
    }

    partial void OnShowFavoriteOnlyChanged(bool value)
    {
        FilterCocktails();
    }

    partial void OnNameSearchTextChanged(string value)
    {
        FilterCocktails();
    }

    partial void OnIngredientSearchTextChanged(string value)
    {
        FilteredIngredients = _allTrackedIngredients
            .Where(nameVm => string.IsNullOrWhiteSpace(value)
                             || nameVm.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
            .ToObservableCollection();
    }
}
