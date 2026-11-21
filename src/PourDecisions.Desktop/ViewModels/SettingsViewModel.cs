using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using PourDecisions.Application.Models;
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
            int? conflictId = null;
            string? newName = null;
            _ = await dialogService.ShowInputDialogAsync($"Rename ingredient type '{ingredientVm.Name}'",
                "Enter new name:", "Confirm", "Cancel", ingredientVm.Name,
                async (value) =>
                {
                    var result = await ingredientService.RenameIngredientAsync(ingredientVm.Id, value);

                    switch (result)
                    {
                        case RenameIngredientResult.Success success:
                            newName = success.NewName;
                            ingredientVm.Name = success.NewName;
                            IngredientTypes.MoveInSorted(ingredientVm, ModifyIngredientTypeViewModel.NameComparer);
                            return Result.Success();
                        case RenameIngredientResult.NameCollisionCanMerge collision:
                            newName = collision.NewName;
                            conflictId = collision.ConflictId;
                            return Result.Success();
                        case RenameIngredientResult.NameCollisionMergeBlocked collision:
                            return Result.Error(
                                $"""
                                 An ingredient type with that name already exists. 
                                 Merging is blocked because both ingredient types are used in cocktails:
                                 {string.Join(Environment.NewLine, collision.CocktailNames.Select(c => $" - {c}"))}
                                 """);
                        default:
                            var errorMessage = result is RenameIngredientResult.Error error
                                ? error.ErrorMessage
                                : "Unknown error occurred.";
                            return Result.Error(errorMessage);
                    }
                });

            if (conflictId is null || newName is null)
            {
                return;
            }

            var dialogResult = await dialogService.ShowConfirmationDialogAsync("Merge ingredient types",
                $"An ingredient type with the name '{newName}' already exists. Do you want to merge '{ingredientVm.Name}' into the existing ingredient type?",
                "Merge", "Cancel", FAContentDialogButton.Close);

            if (dialogResult == FAContentDialogResult.Primary)
            {
                var result = await ingredientService.MergeIngredientTypesAsync(ingredientVm.Id, conflictId.Value);

                switch (result)
                {
                    case MergeIngredientResult.Success success:
                        IngredientTypes.FirstOrDefault(type => type.Id == conflictId.Value)?.IsTracked =
                            success.IsTargetTracked;
                        IngredientTypes.Remove(ingredientVm);
                        return;
                    case MergeIngredientResult.RecipeConflict conflict:
                        await dialogService.ShowInformationDialogAsync("Failed to merge ingredient types",
                            $"""
                             Cannot merge ingredient types. Both ingredient types are used in cocktails:
                             {string.Join(Environment.NewLine, conflict.CocktailNames.Select(c => $" - {c}"))}
                             """);
                        return;
                    default:
                        var errorMessage = result is MergeIngredientResult.Error error
                            ? error.ErrorMessage
                            : "Unknown error occurred.";

                        await dialogService.ShowInformationDialogAsync("Failed to merge ingredient types",
                            errorMessage);
                        return;
                }
            }
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
}
