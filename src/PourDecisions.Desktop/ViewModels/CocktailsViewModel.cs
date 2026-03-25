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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResults))]
    [NotifyPropertyChangedFor(nameof(ShowClearButton))]
    private ObservableCollection<CocktailSummaryViewModel> _filteredCocktails = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private CocktailSummaryViewModel? _selectedCocktail;

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

        FilteredCocktails = _allCocktails.ToObservableCollection();

        SelectedCocktail = FilteredCocktails.FirstOrDefault();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        ShowAvailableOnly = false;
        ShowFavoriteOnly = false;
    }

    private void OnFavoriteToggled(int cocktailId, bool isFavorite)
    {
        _ = SetFavoriteAsync(cocktailId, isFavorite);
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
                (!ShowAvailableOnly || vm.AvailabilityStatus == AvailabilityStatus.Available) &&
                (!ShowFavoriteOnly || vm.IsFavorite) &&
                (string.IsNullOrWhiteSpace(SearchText) ||
                 vm.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)))
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

    partial void OnSearchTextChanged(string value)
    {
        FilterCocktails();
    }
}
