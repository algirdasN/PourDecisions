using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using PourDecisions.Application.Models;
using PourDecisions.Application.Services;
using PourDecisions.Desktop.Services;
using PourDecisions.Shared.Extensions;

namespace PourDecisions.Desktop.ViewModels;

public partial class EditCocktailsViewModel(
    ICocktailService cocktailService,
    IDialogService dialogService,
    IIngredientService ingredientService)
    : ViewModelBase, IAsyncLoadable
{
    [ObservableProperty]
    private CocktailEditItemViewModel? _cocktailEditItemViewModel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasExistingCocktails))]
    private ObservableCollection<CocktailEditSummary> _cocktailSummaries = [];

    private ObservableCollection<string> _ingredientTypeNames = [];

    private bool _isBusy;

    private CocktailEditSummary? _lastSelectedCocktail;

    [ObservableProperty]
    private CocktailEditSummary? _selectedCocktail;

    public bool HasExistingCocktails => CocktailSummaries.Count > 1;

    public string EmptyStateMessage => "No cocktails available. Add cocktails using the form on the right.";

    public async Task LoadAsync()
    {
        var cocktailsTask = cocktailService.GetAllSummariesAsync();
        var ingredientTypeTask = ingredientService.GetIngredientTypeNamesAsync();

        await Task.WhenAll(cocktailsTask, ingredientTypeTask);

        _ingredientTypeNames = ingredientTypeTask.Result.ToObservableCollection();

        CocktailSummaries = cocktailsTask.Result
            .Prepend(new CocktailEditSummary(null, "<New Cocktail>", false))
            .ToObservableCollection();

        CocktailSummaries.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasExistingCocktails));

        SelectedCocktail = CocktailSummaries[0];
    }

    private void OnSaveCocktailClicked(int? id, string name, IList<CocktailIngredientSummary> ingredients,
        string instructions)
    {
        _ = id is null
            ? AddNewCocktailAsync(name, ingredients, instructions)
            : EditCocktailAsync(id.Value, name, ingredients, instructions);
    }

    private async Task AddNewCocktailAsync(string name, IList<CocktailIngredientSummary> ingredients,
        string instructions)
    {
        try
        {
            var newId = await cocktailService.AddCocktailAsync(name, ingredients, instructions);
            var newSummary = await cocktailService.GetSummaryAsync(newId);

            CocktailEditItemViewModel = null;
            CocktailSummaries.InsertIntoSorted(newSummary, CocktailEditSummary.NameComparer);
            SelectedCocktail = newSummary;
        }
        catch (Exception e)
        {
            await dialogService.ShowInformationDialogAsync("Failed to add new cocktail", e.Message);
        }
    }

    private async Task EditCocktailAsync(int id, string name, IList<CocktailIngredientSummary> ingredients,
        string instructions)
    {
        try
        {
            await cocktailService.EditCocktailAsync(id, name, ingredients, instructions);

            var oldSummary = CocktailSummaries.First(summary => summary.Id == id);
            var newSummary = await cocktailService.GetSummaryAsync(id);

            CocktailEditItemViewModel = null;
            CocktailSummaries[CocktailSummaries.IndexOf(oldSummary)] = newSummary;
            SelectedCocktail = newSummary;
        }
        catch (Exception e)
        {
            await dialogService.ShowInformationDialogAsync("Failed to edit cocktail", e.Message);
        }
    }

    private async Task ChangeSelectedCocktailAsync(int? id)
    {
        _isBusy = true;
        try
        {
            if (await GetConfirmationAsync())
            {
                await LoadCocktailFormAsync(id);
                return;
            }

            if (_lastSelectedCocktail is not null)
            {
                SelectedCocktail = _lastSelectedCocktail;
                _lastSelectedCocktail = null;
            }
        }
        catch (Exception e)
        {
            await dialogService.ShowInformationDialogAsync("Failed to switch cocktails", e.Message);
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task<bool> GetConfirmationAsync()
    {
        if (CocktailEditItemViewModel is null || !CocktailEditItemViewModel.IsDirty)
        {
            return true;
        }

        var result = await dialogService.ShowConfirmationDialogAsync("Unsaved changes",
            "Are you sure you want to discard changes?", "Discard", "Cancel");

        return result == ContentDialogResult.Primary;
    }

    private async Task LoadCocktailFormAsync(int? cocktailId)
    {
        try
        {
            CocktailEditItemViewModel?.SaveCocktailClicked -= OnSaveCocktailClicked;
            CocktailEditItemViewModel?.OnDeleteCocktailClicked -= OnDeleteCocktailClicked;

            var cocktail = cocktailId is not null
                ? await cocktailService.GetWithIngredientsAsync(cocktailId.Value)
                : null;

            CocktailEditItemViewModel = new CocktailEditItemViewModel(_ingredientTypeNames, cocktail);
            CocktailEditItemViewModel.SaveCocktailClicked += OnSaveCocktailClicked;
            CocktailEditItemViewModel.OnDeleteCocktailClicked += OnDeleteCocktailClicked;
        }
        catch (Exception e)
        {
            await dialogService.ShowInformationDialogAsync("Failed to load cocktail", e.Message);
        }
    }

    private void OnDeleteCocktailClicked(int cocktailId, string cocktailName)
    {
        _ = DeleteCocktailAsync(cocktailId, cocktailName);
    }

    private async Task DeleteCocktailAsync(int cocktailId, string cocktailName)
    {
        try
        {
            var message = $"Are you sure you want to delete cocktail '{cocktailName}'?";

            var result = await dialogService.ShowConfirmationDialogAsync("Delete cocktail", message,
                "Delete", "Cancel");

            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            await cocktailService.DeleteCocktailAsync(cocktailId);

            CocktailEditItemViewModel = null;
            CocktailSummaries.Remove(CocktailSummaries.First(cocktail => cocktail.Id == cocktailId));
            SelectedCocktail = CocktailSummaries[0];
        }
        catch (Exception e)
        {
            await dialogService.ShowInformationDialogAsync("Failed to delete cocktail", e.Message);
        }
    }

    partial void OnSelectedCocktailChanging(CocktailEditSummary? value)
    {
        _lastSelectedCocktail = SelectedCocktail;
    }

    partial void OnSelectedCocktailChanged(CocktailEditSummary? value)
    {
        if (_isBusy)
        {
            return;
        }

        _ = ChangeSelectedCocktailAsync(value?.Id);
    }
}
