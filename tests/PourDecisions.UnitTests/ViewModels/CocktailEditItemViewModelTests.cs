using System.Collections.ObjectModel;
using PourDecisions.Application.Models;
using PourDecisions.Core.Entities;
using PourDecisions.Desktop.ViewModels;

namespace PourDecisions.UnitTests.ViewModels;

public class CocktailEditItemViewModelTests
{
    private readonly ObservableCollection<string> _ingredientTypeNames = ["Gin", "Vodka", "Vermouth"];

    [Fact]
    public void Constructor_WithNull_SetsDefaultValues()
    {
        // Act
        var viewModel = new CocktailEditItemViewModel(_ingredientTypeNames, null);

        // Assert
        Assert.Equal("Add new cocktail", viewModel.Header);
        Assert.Equal("Add", viewModel.SaveButtonLabel);
        Assert.False(viewModel.CanDeleteCocktail);
        Assert.Single(viewModel.Ingredients);
        Assert.True(viewModel.Ingredients[0].IsFirst);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public void Constructor_WithCocktail_SetsValues()
    {
        // Arrange
        var cocktail = new Cocktail
        {
            Id = 1,
            Name = "Martini",
            Instructions = "Stir",
            CocktailIngredients =
            [
                new CocktailIngredient { Type = new IngredientType { Name = "Gin" }, AmountValue = 60 }
            ]
        };

        // Act
        var viewModel = new CocktailEditItemViewModel(_ingredientTypeNames, cocktail);

        // Assert
        Assert.Equal("Edit cocktail: 'Martini'", viewModel.Header);
        Assert.Equal("Save", viewModel.SaveButtonLabel);
        Assert.True(viewModel.CanDeleteCocktail);
        Assert.Equal("Martini", viewModel.Name);
        Assert.Equal("Stir", viewModel.Instructions);
        Assert.Single(viewModel.Ingredients);
        Assert.Equal("Martini", viewModel.Name);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public void SetDirty_WhenPropertiesChange()
    {
        // Arrange
        var viewModel = new CocktailEditItemViewModel(_ingredientTypeNames, null);
        Assert.False(viewModel.IsDirty);

        // Act
        viewModel.Name = "New Name";

        // Assert
        Assert.True(viewModel.IsDirty);
    }

    [Fact]
    public void SetDirty_WhenIngredientsChange()
    {
        // Arrange
        var viewModel = new CocktailEditItemViewModel(_ingredientTypeNames, null);
        Assert.False(viewModel.IsDirty);

        // Act
        viewModel.Ingredients[0].AmountText = "10";

        // Assert
        Assert.True(viewModel.IsDirty);
    }

    [Fact]
    public void AddIngredient_AddsNewViewModel()
    {
        // Arrange
        var viewModel = new CocktailEditItemViewModel(_ingredientTypeNames, null);

        // Act
        viewModel.AddIngredientCommand.Execute(null);

        // Assert
        Assert.Equal(2, viewModel.Ingredients.Count);
        Assert.True(viewModel.Ingredients[0].IsFirst);
        Assert.False(viewModel.Ingredients[1].IsFirst);
    }

    [Fact]
    public void DeleteIngredient_RemovesViewModel_AndMaintainsAtLeastOne()
    {
        // Arrange
        var viewModel = new CocktailEditItemViewModel(_ingredientTypeNames, null);
        viewModel.AddIngredientCommand.Execute(null);
        var ingredientToDelete = viewModel.Ingredients[0];

        // Act
        ingredientToDelete.DeleteCommand.Execute(null);

        // Assert
        Assert.Single(viewModel.Ingredients);
        Assert.NotEqual(ingredientToDelete, viewModel.Ingredients[0]);
        Assert.True(viewModel.Ingredients[0].IsFirst);

        // Act - delete last one
        viewModel.Ingredients[0].DeleteCommand.Execute(null);
        Assert.Single(viewModel.Ingredients); // Should recreate one
        Assert.True(viewModel.Ingredients[0].IsFirst);
    }

    [Fact]
    public void SaveCocktail_InvokesEvent_WhenValid()
    {
        // Arrange
        var viewModel = new CocktailEditItemViewModel(_ingredientTypeNames, null)
        {
            Name = "Valid Name",
            Instructions = "Valid Instructions"
        };
        viewModel.Ingredients[0].AmountText = "50";
        viewModel.Ingredients[0].Name = "Gin";
        
        int? capturedId = -1;
        string capturedName = "";
        IList<CocktailIngredientSummary>? capturedIngredients = null;
        string capturedInstructions = "";

        viewModel.SaveCocktailClicked += (id, name, ingredients, instructions) =>
        {
            capturedId = id;
            capturedName = name;
            capturedIngredients = ingredients;
            capturedInstructions = instructions;
        };

        // Act
        viewModel.SaveCocktailCommand.Execute(null);

        // Assert
        Assert.Null(capturedId);
        Assert.Equal("Valid Name", capturedName);
        Assert.Single(capturedIngredients!);
        Assert.Equal("Gin", capturedIngredients?[0].Name);
        Assert.Equal(50, capturedIngredients?[0].Amount);
        Assert.Equal("Valid Instructions", capturedInstructions);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public void SaveCocktail_DoesNotInvokeEvent_WhenInvalid()
    {
        // Arrange
        var viewModel = new CocktailEditItemViewModel(_ingredientTypeNames, null)
        {
            Name = "Valid Name"
        };
        viewModel.Ingredients[0].AmountText = "invalid"; // Invalid amount

        var invoked = false;
        viewModel.SaveCocktailClicked += (id, name, ingredients, instructions) => invoked = true;

        // Act
        viewModel.SaveCocktailCommand.Execute(null);

        // Assert
        Assert.False(invoked);
        Assert.True(viewModel.IsDirty); // remains dirty as it failed
        Assert.NotNull(viewModel.AmountError);
    }

    [Fact]
    public void DeleteCocktail_InvokesEvent()
    {
        // Arrange
        var cocktail = new Cocktail { Id = 1, Name = "Martini" };
        var viewModel = new CocktailEditItemViewModel(_ingredientTypeNames, cocktail);

        int capturedId = -1;
        string capturedName = "";
        viewModel.DeleteCocktailClicked += (id, name) =>
        {
            capturedId = id;
            capturedName = name;
        };

        // Act
        viewModel.DeleteCocktailCommand.Execute(null);

        // Assert
        Assert.Equal(1, capturedId);
        Assert.Equal("Martini", capturedName);
    }

    [Fact]
    public void DuplicateCheck_DetectsDuplicateNames()
    {
        // Arrange
        var viewModel = new CocktailEditItemViewModel(_ingredientTypeNames, null);
        viewModel.AddIngredientCommand.Execute(null);
        
        // Act
        viewModel.Ingredients[0].Name = "Gin";
        viewModel.Ingredients[1].Name = "Gin";

        // Assert
        Assert.True(viewModel.Ingredients[0].HasDuplicateName);
        Assert.True(viewModel.Ingredients[1].HasDuplicateName);

        // Act - change one
        viewModel.Ingredients[1].Name = "Vodka";
        Assert.False(viewModel.Ingredients[0].HasDuplicateName);
        Assert.False(viewModel.Ingredients[1].HasDuplicateName);
    }

    [Fact]
    public void Navigation_MovesIngredients()
    {
        // Arrange
        var viewModel = new CocktailEditItemViewModel(_ingredientTypeNames, null);
        viewModel.AddIngredientCommand.Execute(null);
        viewModel.Ingredients[0].Name = "First";
        viewModel.Ingredients[1].Name = "Second";

        // Act
        viewModel.Ingredients[0].NavigateCommand.Execute(null);

        // Assert
        Assert.Equal("Second", viewModel.Ingredients[0].Name);
        Assert.Equal("First", viewModel.Ingredients[1].Name);
        Assert.True(viewModel.Ingredients[0].IsFirst);
        Assert.False(viewModel.Ingredients[1].IsFirst);
    }
}
