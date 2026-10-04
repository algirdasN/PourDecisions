using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
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
    IIngredientService ingredientService,
    IDialogService dialogService)
    : ViewModelBase, IAsyncLoadable
{
    private ICollection<Bottle> _bottles = [];

    [ObservableProperty]
    public partial ObservableCollection<ModifyIngredientTypeViewModel> IngredientTypes { get; set; } = [];

    public string AboutText => $"Version: {Assembly.GetExecutingAssembly().GetName().Version?.ToString()}";

    public async Task LoadAsync()
    {
        _bottles = await bottleService.GetBottlesWithTypeAsync();
        var ingredientTypes = await ingredientService.GetIngredientTypesAsync();

        IngredientTypes = ingredientTypes
            .Select(ingredientType =>
            {
                var vm = new ModifyIngredientTypeViewModel(ingredientType)
                {
                    RequestTrackedChange = ChangeIngredientTrackedStatus
                };
                vm.RenameButtonClicked += OnRenameButtonClicked;
                vm.DeleteButtonClicked += OnDeleteButtonClicked;
                return vm;
            })
            .ToObservableCollection();
    }

    private async Task<bool> ChangeIngredientTrackedStatus(ModifyIngredientTypeViewModel ingredientVm, bool newValue)
    {
        if (!newValue)
        {
            var bottlesWithIngredient = _bottles.Where(bottle => bottle.TypeId == ingredientVm.Id).ToList();

            if (bottlesWithIngredient.Count > 0)
            {
                var result = await dialogService.ShowConfirmationDialogAsync("Set ingredient type to untracked",
                    $"""
                     Confirm setting {ingredientVm.Name} to untracked. The following bottles will be deleted:
                     {string.Join(Environment.NewLine, bottlesWithIngredient.Select(b => $" - {b.Name} ({b.Volume} ml)"))}
                     """,
                    "Confirm", "Cancel", FAContentDialogButton.Close);

                if (result != FAContentDialogResult.Primary)
                {
                    return false;
                }
            }
        }

        try
        {
            await ingredientService.UpdateIngredientTypeTrackedWithBottleCleanupAsync(ingredientVm.Id, newValue);
            _bottles = await bottleService.GetBottlesWithTypeAsync();
            return true;
        }
        catch (Exception ex)
        {
            await dialogService.ShowInformationDialogAsync("Failed to change tracked status", ex.Message);
            return false;
        }
    }

    private void OnRenameButtonClicked(ModifyIngredientTypeViewModel ingredientVm)
    {
        _ = RenameIngredientType(ingredientVm);
    }

    private async Task RenameIngredientType(ModifyIngredientTypeViewModel ingredientVm)
    {
        var newName = await dialogService.ShowInputDialogAsync("Rename ingredient type", "Enter new name:", "Confirm",
            "Cancel", ingredientVm.Name, BuildIngredientNameValidator(ingredientVm.Name));

        if (newName is null)
        {
            return;
        }

        try
        {
            await ingredientService.RenameIngredientAsync(ingredientVm.Id, newName);

            ingredientVm.Name = newName.ToTitleCase();

            IngredientTypes.MoveInSorted(ingredientVm, ModifyIngredientTypeViewModel.NameComparer);
        }
        catch (Exception e)
        {
            await dialogService.ShowInformationDialogAsync("Failed to rename ingredient type", e.Message);
        }
    }

    private void OnDeleteButtonClicked(ModifyIngredientTypeViewModel ingredientVm)
    {
        _ = DeleteIngredientType(ingredientVm);
    }

    private async Task DeleteIngredientType(ModifyIngredientTypeViewModel ingredientVm)
    {
        var deleteImpact = await ingredientService.PreviewDeleteIngredientTypeAsync(ingredientVm.Id);

        if (deleteImpact.CocktailNames.Count > 0)
        {
            await dialogService.ShowInformationDialogAsync("Cannot delete ingredient type",
                $"""
                 This ingredient type is used in one or more cocktails:
                 {string.Join(Environment.NewLine, deleteImpact.CocktailNames.Select(c => $" - {c}"))}
                 """);

            return;
        }

        var message = deleteImpact.BottleInfoList.Count > 0
            ? $"""
               Confirm ingredient type deletion. The following bottles will also be deleted:
               {string.Join(Environment.NewLine, deleteImpact.BottleInfoList.Select(b => $" - {b}"))}
               """
            : "Confirm ingredient type deletion.";

        var dialogResult = await dialogService.ShowConfirmationDialogAsync("Delete ingredient type", message, "Delete",
            "Cancel", FAContentDialogButton.Close);

        if (dialogResult != FAContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            await ingredientService.DeleteIngredientTypeAsync(ingredientVm.Id, true);

            IngredientTypes.Remove(IngredientTypes.First(type => type.Id == ingredientVm.Id));
        }
        catch (Exception e)
        {
            await dialogService.ShowInformationDialogAsync("Failed to delete ingredient type", e.Message);
        }
    }

    private Func<string, ValidationResult?> BuildIngredientNameValidator(string ingredientName)
    {
        return value =>
        {
            if (value.Length < 3)
            {
                return new ValidationResult("Ingredient type name must be at least 3 characters long.");
            }

            if (value.Equals(ingredientName, StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult("Enter a new ingredient type name.");
            }

            if (IngredientTypes.Any(type => type.Name.Equals(value, StringComparison.OrdinalIgnoreCase)))
            {
                return new ValidationResult("Ingredient type name is already in use.");
            }

            return ValidationResult.Success;
        };
    }
}
