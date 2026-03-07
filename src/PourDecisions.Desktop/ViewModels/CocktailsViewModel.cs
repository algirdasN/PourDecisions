using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PourDecisions.Application.AvailabilityEngine;
using PourDecisions.Application.Services;

namespace PourDecisions.Desktop.ViewModels;

public partial class CocktailsViewModel(ICocktailService cocktailService, IAvailabilityService availabilityService)
    : ViewModelBase, IAsyncLoadable
{
    public const string ViewName = "Cocktails";

    private readonly List<CocktailsSummaryViewModel> _allCocktails = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResults))]
    [NotifyPropertyChangedFor(nameof(ShowClearButton))]
    private ObservableCollection<CocktailsSummaryViewModel> _filteredCocktails = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private CocktailsSummaryViewModel? _selectedCocktail;

    [ObservableProperty]
    private bool _showAvailableOnly;

    [ObservableProperty]
    private bool _showFavoriteOnly;

    public string Title => ViewName;
    public bool HasResults => FilteredCocktails.Count > 0;
    public bool ShowClearButton => _allCocktails.Count > 0 && FilteredCocktails.Count == 0;

    public string EmptyStateMessage => _allCocktails.Count == 0
        ? "No cocktails available. Please add some cocktails in the Edit Cocktails page."
        : "No cocktails match the current filters. Try adjusting the filters or search text.";

    public async Task LoadAsync()
    {
        var cocktailTask = cocktailService.GetAllWithIngredientsAsync();
        var availabilityTask = availabilityService.GetCocktailAvailabilityAsync();

        _allCocktails.Clear();

        await Task.WhenAll(cocktailTask, availabilityTask);

        var cocktails = cocktailTask.Result;
        var cocktailAvailability = availabilityTask.Result;

        foreach (var cocktail in cocktails)
        {
            var vm = new CocktailsSummaryViewModel(cocktail, cocktailAvailability[cocktail.Id]);
            vm.FavoriteToggled += OnFavoriteToggled;
            _allCocktails.Add(vm);
        }

        FilteredCocktails = new ObservableCollection<CocktailsSummaryViewModel>(_allCocktails);

        SelectedCocktail = FilteredCocktails.FirstOrDefault();
    }

    [RelayCommand]
    public void ClearFiltersCommand()
    {
        SearchText = string.Empty;
        ShowAvailableOnly = false;
        ShowFavoriteOnly = false;
    }

    private void OnFavoriteToggled(int cocktailId, bool isFavorite)
    {
        _ = cocktailService.SetFavoriteAsync(cocktailId, isFavorite);
        FilterCocktails();
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

    private void FilterCocktails()
    {
        var filtered = _allCocktails.Where(vm =>
            (!ShowAvailableOnly || vm.AvailabilityStatus == AvailabilityStatus.Available) &&
            (!ShowFavoriteOnly || vm.IsFavorite) &&
            (string.IsNullOrWhiteSpace(SearchText) ||
             vm.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

        FilteredCocktails = new ObservableCollection<CocktailsSummaryViewModel>(filtered);

        if (SelectedCocktail == null || !FilteredCocktails.Contains(SelectedCocktail))
        {
            SelectedCocktail = FilteredCocktails.FirstOrDefault();
        }
    }
}
