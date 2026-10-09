using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using PourDecisions.Application.Services;
using PourDecisions.Desktop.Services;
using PourDecisions.Shared.Extensions;

namespace PourDecisions.Desktop.ViewModels;

public partial class SettingsViewModel(IIngredientService ingredientService, IDialogService dialogService)
    : ViewModelBase, IAsyncLoadable
{
    [ObservableProperty]
    public partial ObservableCollection<ModifyIngredientTypeViewModel> IngredientTypes { get; set; } = [];

    public string AboutText => $"Version: {Assembly.GetExecutingAssembly().GetName().Version?.ToString()}";

    public async Task LoadAsync()
    {
        var ingredientTypes = await ingredientService.GetIngredientTypesAsync();

        IngredientTypes = ingredientTypes
            .Select(ingredientType =>
            {
                var vm = new ModifyIngredientTypeViewModel(ingredientType);
                vm.ToggleTrackedButtonClicked += OnToggleTrackedButtonClicked;
                vm.RenameButtonClicked += OnRenameButtonClicked;
                vm.DeleteButtonClicked += OnDeleteButtonClicked;
                return vm;
            })
            .ToObservableCollection();
    }

    private void OnToggleTrackedButtonClicked(ModifyIngredientTypeViewModel ingredientVm, bool newValue)
    {
        _ = ChangeIngredientTrackedStatus(ingredientVm, !ingredientVm.IsTracked);
    }

    private async Task ChangeIngredientTrackedStatus(ModifyIngredientTypeViewModel ingredientVm, bool newValue)
    {
        ingredientVm.IsBusy = true;

        try
        {
            if (!newValue)
            {
                var modifyImpact = await ingredientService.PreviewIngredientTypeModifyAsync(ingredientVm.Id);

                if (modifyImpact.BottleInfoList.Count > 0)
                {
                    var result = await dialogService.ShowConfirmationDialogAsync("Set ingredient type to untracked",
                        $"""
                         Confirm setting {ingredientVm.Name} to untracked. The following bottles will be deleted:
                         {string.Join(Environment.NewLine, modifyImpact.BottleInfoList.Select(b => $" - {b.Name} ({b.Volume} ml)"))}
                         """,
                        "Confirm", "Cancel", FAContentDialogButton.Close);

                    if (result != FAContentDialogResult.Primary)
                    {
                        return;
                    }
                }
            }

            await ingredientService.UpdateIngredientTypeTrackedAsync(ingredientVm.Id, newValue, true);
            ingredientVm.IsTracked = newValue;
        }
        catch (Exception ex)
        {
            await dialogService.ShowInformationDialogAsync("Failed to change tracked status", ex.Message);
        }
        finally
        {
            ingredientVm.IsBusy = false;
        }
    }

    private void OnRenameButtonClicked(ModifyIngredientTypeViewModel ingredientVm)
    {
        _ = RenameIngredientType(ingredientVm);
    }

    private async Task RenameIngredientType(ModifyIngredientTypeViewModel ingredientVm)
    {
        ingredientVm.IsBusy = true;

        try
        {
            var newName = await dialogService.ShowInputDialogAsync("Rename ingredient type", "Enter new name:",
                "Confirm", "Cancel", ingredientVm.Name, BuildIngredientNameValidator(ingredientVm.Name));

            if (newName is null)
            {
                return;
            }

            await ingredientService.RenameIngredientAsync(ingredientVm.Id, newName);

            ingredientVm.Name = newName.ToTitleCase();

            IngredientTypes.MoveInSorted(ingredientVm, ModifyIngredientTypeViewModel.NameComparer);
        }
        catch (Exception e)
        {
            await dialogService.ShowInformationDialogAsync("Failed to rename ingredient type", e.Message);
        }
        finally
        {
            ingredientVm.IsBusy = false;
        }
    }

    private void OnDeleteButtonClicked(ModifyIngredientTypeViewModel ingredientVm)
    {
        _ = DeleteIngredientType(ingredientVm);
    }

    private async Task DeleteIngredientType(ModifyIngredientTypeViewModel ingredientVm)
    {
        ingredientVm.IsBusy = true;

        try
        {
            var deleteImpact = await ingredientService.PreviewIngredientTypeModifyAsync(ingredientVm.Id);

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
                   {string.Join(Environment.NewLine, deleteImpact.BottleInfoList.Select(b => $" - {b.Name} ({b.Volume} ml)"))}
                   """
                : "Confirm ingredient type deletion.";

            var dialogResult = await dialogService.ShowConfirmationDialogAsync("Delete ingredient type", message,
                "Delete", "Cancel", FAContentDialogButton.Close);

            if (dialogResult != FAContentDialogResult.Primary)
            {
                return;
            }

            await ingredientService.DeleteIngredientTypeAsync(ingredientVm.Id, true);

            IngredientTypes.Remove(IngredientTypes.First(type => type.Id == ingredientVm.Id));
        }
        catch (Exception e)
        {
            await dialogService.ShowInformationDialogAsync("Failed to delete ingredient type", e.Message);
        }
        finally
        {
            ingredientVm.IsBusy = false;
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
