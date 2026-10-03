using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using PourDecisions.Application.Services;
using PourDecisions.Core.Entities;
using PourDecisions.Desktop.Services;
using PourDecisions.Shared.Extensions;

namespace PourDecisions.Desktop.ViewModels;

public partial class SettingsViewModel(
    IBottleService bottleService,
    ICocktailService cocktailService,
    IIngredientService ingredientService,
    IDialogService dialogService)
    : ViewModelBase, IAsyncLoadable
{
    private ICollection<Bottle> _bottles = [];
    private ICollection<Cocktail> _cocktails = [];

    [ObservableProperty]
    public partial ObservableCollection<ModifyIngredientTypeViewModel> IngredientTypes { get; set; } = [];

    public string AboutText => $"Version: {Assembly.GetExecutingAssembly().GetName().Version?.ToString()}";

    public async Task LoadAsync()
    {
        var bottleTask = bottleService.GetAllBottlesAsync();
        var cocktailsTask = cocktailService.GetAllWithIngredientsAsync();

        IngredientTypes = (await ingredientService.GetIngredientTypesAsync())
            .Select(ingredientType =>
            {
                var vm = new ModifyIngredientTypeViewModel(ingredientType);
                vm.RenameButtonClicked += OnRenameButtonClicked;
                vm.DeleteButtonClicked += OnDeleteButtonClicked;
                return vm;
            })
            .ToObservableCollection();

        _bottles = await bottleTask;
        _cocktails = await cocktailsTask;
    }

    private void OnRenameButtonClicked(int ingredientTypeId)
    {
        
    }

    private void OnDeleteButtonClicked(int ingredientTypeId)
    {
        _ = DeleteIngredientType(ingredientTypeId);
    }

    private async Task DeleteIngredientType(int ingredientTypeId)
    {
        var cocktailsWithIngredient = _cocktails
            .Where(cocktail => cocktail.CocktailIngredients.Any(i => i.TypeId == ingredientTypeId))
            .ToList();

        if (cocktailsWithIngredient.Count > 0)
        {
            await dialogService.ShowInformationDialogAsync("Cannot delete ingredient type",
                $"""
                 This ingredient type is used in one or more cocktails:
                 {string.Join(", ", cocktailsWithIngredient.Select(c => c.Name))}
                 """);

            return;
        }

        var bottlesWithIngredient = _bottles.Where(bottle => bottle.TypeId == ingredientTypeId).ToList();

        var message = bottlesWithIngredient.Count > 0
            ? $"""
               Confirm ingredient type deletion. The following bottles will also be deleted:
               {string.Join(", ", bottlesWithIngredient.Select(b => $"{b.Name} ({b.Volume} ml)"))}
               """
            : "Confirm ingredient type deletion.";

        var dialogResult =
            await dialogService.ShowConfirmationDialogAsync("Delete ingredient type", message, "Delete", "Cancel");

        if (dialogResult != FAContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            await ingredientService.DeleteIngredientTypeAsync(ingredientTypeId);

            IngredientTypes.Remove(IngredientTypes.First(type => type.Id == ingredientTypeId));
        }
        catch (Exception e)
        {
            await dialogService.ShowInformationDialogAsync("Failed to delete ingredient type", e.Message);
        }
    }
}
