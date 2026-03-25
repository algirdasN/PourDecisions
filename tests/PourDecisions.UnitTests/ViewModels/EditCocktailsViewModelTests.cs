using NSubstitute;
using PourDecisions.Application.Models;
using PourDecisions.Application.Services;
using PourDecisions.Core.Entities;
using PourDecisions.Desktop.Services;
using PourDecisions.Desktop.ViewModels;
using FluentAvalonia.UI.Controls;

namespace PourDecisions.UnitTests.ViewModels;

public class EditCocktailsViewModelTests
{
    private readonly ICocktailService _cocktailService = Substitute.For<ICocktailService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly IIngredientService _ingredientService = Substitute.For<IIngredientService>();

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

        var viewModel = new EditCocktailsViewModel(_cocktailService, _dialogService, _ingredientService);

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
        _cocktailService.GetWithIngredientsAsync(1).Returns(new Cocktail { Id = 1, Name = "Martini", CocktailIngredients = new List<CocktailIngredient>() });

        var viewModel = new EditCocktailsViewModel(_cocktailService, _dialogService, _ingredientService);
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
        _cocktailService.AddCocktailAsync(Arg.Any<string>(), Arg.Any<IList<CocktailIngredientSummary>>(), Arg.Any<string>()).Returns(2);
        var newSummary = new CocktailEditSummary(2, "New One", false);
        _cocktailService.GetSummaryAsync(2).Returns(newSummary);

        var viewModel = new EditCocktailsViewModel(_cocktailService, _dialogService, _ingredientService);
        await viewModel.LoadAsync();
        
        // Trigger loading the form for <New Cocktail>
        viewModel.SelectedCocktail = viewModel.CocktailSummaries[0];
        await Task.Delay(100);

        // Act
        // Simulate SaveCocktailClicked event from the child view model
        var saveMethod = viewModel.GetType().GetMethod("OnSaveCocktailClicked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        saveMethod!.Invoke(viewModel, [null, "New One", new List<CocktailIngredientSummary>(), "Instructions"]);

        await Task.Delay(100);

        // Assert
        await _cocktailService.Received(1).AddCocktailAsync("New One", Arg.Any<IList<CocktailIngredientSummary>>(), "Instructions");
        Assert.Contains(newSummary, viewModel.CocktailSummaries);
        Assert.Equal(newSummary, viewModel.SelectedCocktail);
    }

    [Fact]
    public async Task DeleteCocktail_RemovesFromList_AfterConfirmation()
    {
        // Arrange
        var summary = new CocktailEditSummary(1, "Martini", false);
        _cocktailService.GetAllSummariesAsync().Returns(new List<CocktailEditSummary> { summary });
        _ingredientService.GetIngredientTypeNamesAsync().Returns(new List<string>());
        _cocktailService.GetWithIngredientsAsync(1).Returns(new Cocktail { Id = 1, Name = "Martini", CocktailIngredients = new List<CocktailIngredient>() });
        _dialogService.ShowConfirmationDialogAsync(Arg.Any<string>(), Arg.Any<string>(), "Delete", "Cancel")
            .Returns(ContentDialogResult.Primary);

        var viewModel = new EditCocktailsViewModel(_cocktailService, _dialogService, _ingredientService);
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
    }
}
