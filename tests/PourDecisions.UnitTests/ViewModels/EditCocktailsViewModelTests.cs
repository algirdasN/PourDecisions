using System.Reflection;
using FluentAvalonia.UI.Controls;
using NSubstitute;
using PourDecisions.Application.Models;
using PourDecisions.Application.Services;
using PourDecisions.Core.Entities;
using PourDecisions.Desktop.Services;
using PourDecisions.Desktop.ViewModels;

namespace PourDecisions.UnitTests.ViewModels;

public class EditCocktailsViewModelTests
{
    private readonly ICocktailService _cocktailService = Substitute.For<ICocktailService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly IIngredientService _ingredientService = Substitute.For<IIngredientService>();
    private readonly IToastService _toastService = Substitute.For<IToastService>();

    [Fact]
    public async Task LoadAsync_LoadsSummaries_AndSetsInitialSelection()
    {
        // Arrange
        var summaries = new List<CocktailEditSummary>
        {
            new(1, "Martini", false)
        };
        _cocktailService.GetAllSummariesAsync().Returns(summaries);
        _ingredientService.GetIngredientTypeNamesAsync().Returns(new List<string> { "Gin" });

        var viewModel = new EditCocktailsViewModel(_cocktailService, _dialogService, _ingredientService, _toastService);

        // Act
        await viewModel.LoadAsync();

        // Assert
        Assert.Equal(2, viewModel.CocktailSummaries.Count); // <New Cocktail> + Martini
        Assert.Equal("<New Cocktail>", viewModel.CocktailSummaries[0].Name);
        Assert.Equal(viewModel.CocktailSummaries[0], viewModel.SelectedCocktail);
        Assert.True(viewModel.HasExistingCocktails);
    }

    [Fact]
    public async Task ChangeSelectedCocktail_LoadsForm_WhenConfirmed()
    {
        // Arrange
        var summaries = new List<CocktailEditSummary>
        {
            new(1, "Martini", false)
        };
        _cocktailService.GetAllSummariesAsync().Returns(summaries);
        _ingredientService.GetIngredientTypeNamesAsync().Returns(new List<string> { "Gin" });
        _cocktailService.GetWithIngredientsAsync(1).Returns(new Cocktail
            { Id = 1, Name = "Martini", CocktailIngredients = new List<CocktailIngredient>() });

        var viewModel = new EditCocktailsViewModel(_cocktailService, _dialogService, _ingredientService, _toastService);
        await viewModel.LoadAsync();

        // Act
        viewModel.SelectedCocktail = viewModel.CocktailSummaries[1];

        // Assert
        await Task.Delay(100);

        Assert.NotNull(viewModel.CocktailEditItemViewModel);
        Assert.Equal("Edit cocktail: 'Martini'", viewModel.CocktailEditItemViewModel.Header);
    }

    [Fact]
    public async Task AddNewCocktail_SavesAndUpdatesList()
    {
        // Arrange
        _cocktailService.GetAllSummariesAsync().Returns(new List<CocktailEditSummary>());
        _ingredientService.GetIngredientTypeNamesAsync().Returns(new List<string>());
        _cocktailService
            .AddCocktailAsync(Arg.Any<string>(), Arg.Any<IList<CocktailIngredientSummary>>(), Arg.Any<string>())
            .Returns(2);
        var newSummary = new CocktailEditSummary(2, "New One", false);
        _cocktailService.GetSummaryAsync(2).Returns(newSummary);

        var viewModel = new EditCocktailsViewModel(_cocktailService, _dialogService, _ingredientService, _toastService);
        await viewModel.LoadAsync();

        // Trigger loading the form for <New Cocktail>
        viewModel.SelectedCocktail = viewModel.CocktailSummaries[0];
        await Task.Delay(100);

        // Act
        // Simulate SaveCocktailClicked event from the child view model
        var saveMethod = viewModel.GetType()
            .GetMethod("OnSaveCocktailClicked", BindingFlags.NonPublic | BindingFlags.Instance);
        saveMethod!.Invoke(viewModel, [null, "New One", new List<CocktailIngredientSummary>(), "Instructions"]);

        await Task.Delay(100);

        // Assert
        await _cocktailService.Received(1)
            .AddCocktailAsync("New One", Arg.Any<IList<CocktailIngredientSummary>>(), "Instructions");
        Assert.Contains(newSummary, viewModel.CocktailSummaries);
        Assert.Equal(newSummary, viewModel.SelectedCocktail);
        _toastService.Received(1).ShowSuccess("Successfully added cocktail 'New One'");
    }

    [Fact]
    public async Task DeleteCocktail_RemovesFromList_AfterConfirmation()
    {
        // Arrange
        var summary = new CocktailEditSummary(1, "Martini", false);
        _cocktailService.GetAllSummariesAsync().Returns(new List<CocktailEditSummary> { summary });
        _ingredientService.GetIngredientTypeNamesAsync().Returns(new List<string>());
        _cocktailService.GetWithIngredientsAsync(1).Returns(new Cocktail
            { Id = 1, Name = "Martini", CocktailIngredients = new List<CocktailIngredient>() });
        _dialogService.ShowConfirmationDialogAsync(Arg.Any<string>(), Arg.Any<string>(), "Delete", "Cancel",
                Arg.Any<FAContentDialogButton>())
            .Returns(FAContentDialogResult.Primary);

        var viewModel = new EditCocktailsViewModel(_cocktailService, _dialogService, _ingredientService, _toastService);
        await viewModel.LoadAsync();

        viewModel.SelectedCocktail = viewModel.CocktailSummaries[1];
        await Task.Delay(100);

        // Act
        viewModel.CocktailEditItemViewModel!.DeleteCocktailCommand.Execute(null);
        await Task.Delay(100);

        // Assert
        await _cocktailService.Received(1).DeleteCocktailAsync(1);
        Assert.Single(viewModel.CocktailSummaries); // Only <New Cocktail> left
        Assert.Equal("<New Cocktail>", viewModel.CocktailSummaries[0].Name);
        _toastService.Received(1).ShowSuccess("Successfully deleted cocktail 'Martini'");
    }

    [Fact]
    public async Task EditCocktail_ShowsSuccessAfterSaving()
    {
        var oldSummary = new CocktailEditSummary(1, "Martini", false);
        var newSummary = new CocktailEditSummary(1, "Updated Martini", false);
        _cocktailService.GetAllSummariesAsync().Returns(new List<CocktailEditSummary> { oldSummary });
        _ingredientService.GetIngredientTypeNamesAsync().Returns(new List<string>());
        _cocktailService.GetSummaryAsync(1).Returns(newSummary);
        var viewModel = new EditCocktailsViewModel(_cocktailService, _dialogService, _ingredientService, _toastService);
        await viewModel.LoadAsync();

        await InvokeAsync(viewModel, "EditCocktailAsync", 1, "Updated Martini",
            new List<CocktailIngredientSummary>(), "Stir");

        await _cocktailService.Received(1).EditCocktailAsync(1, "Updated Martini",
            Arg.Any<IList<CocktailIngredientSummary>>(), "Stir");
        Assert.Contains(newSummary, viewModel.CocktailSummaries);
        _toastService.Received(1).ShowSuccess("Successfully updated cocktail 'Updated Martini'");
    }

    [Theory]
    [InlineData("add")]
    [InlineData("edit")]
    [InlineData("delete")]
    public async Task FailedAction_DoesNotShowToast(string action)
    {
        var error = new InvalidOperationException("Save failed");
        _cocktailService.AddCocktailAsync(Arg.Any<string>(), Arg.Any<IList<CocktailIngredientSummary>>(), Arg.Any<string>())
            .Returns(Task.FromException<int>(error));
        _cocktailService.EditCocktailAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<IList<CocktailIngredientSummary>>(), Arg.Any<string>())
            .Returns(Task.FromException(error));
        _cocktailService.DeleteCocktailAsync(Arg.Any<int>()).Returns(Task.FromException(error));
        _dialogService.ShowConfirmationDialogAsync(Arg.Any<string>(), Arg.Any<string>(), "Delete", "Cancel",
            Arg.Any<FAContentDialogButton>()).Returns(FAContentDialogResult.Primary);
        var viewModel = new EditCocktailsViewModel(_cocktailService, _dialogService, _ingredientService, _toastService);
        var ingredients = new List<CocktailIngredientSummary>();

        switch (action)
        {
            case "add":
                await InvokeAsync(viewModel, "AddNewCocktailAsync", "Martini", ingredients, "Stir");
                break;
            case "edit":
                await InvokeAsync(viewModel, "EditCocktailAsync", 1, "Martini", ingredients, "Stir");
                break;
            default:
                await InvokeAsync(viewModel, "DeleteCocktailAsync", 1, "Martini");
                break;
        }

        _toastService.DidNotReceive().ShowSuccess(Arg.Any<string>());
        _toastService.DidNotReceive().ShowWarning(Arg.Any<string>());
        await _dialogService.Received(1).ShowInformationDialogAsync(Arg.Any<string>(), "Save failed");
    }

    [Fact]
    public async Task DeleteCocktail_WhenCancelled_DoesNotShowToast()
    {
        _dialogService.ShowConfirmationDialogAsync(Arg.Any<string>(), Arg.Any<string>(), "Delete", "Cancel",
            Arg.Any<FAContentDialogButton>()).Returns(FAContentDialogResult.None);
        var viewModel = new EditCocktailsViewModel(_cocktailService, _dialogService, _ingredientService, _toastService);

        await InvokeAsync(viewModel, "DeleteCocktailAsync", 1, "Martini");

        await _cocktailService.DidNotReceive().DeleteCocktailAsync(Arg.Any<int>());
        _toastService.DidNotReceive().ShowWarning(Arg.Any<string>());
        _toastService.DidNotReceive().ShowSuccess(Arg.Any<string>());
    }

    private static Task InvokeAsync(EditCocktailsViewModel viewModel, string method, params object?[] args)
    {
        return (Task)viewModel.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(viewModel, args)!;
    }
}
