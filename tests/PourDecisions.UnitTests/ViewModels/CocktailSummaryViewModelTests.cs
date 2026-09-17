using PourDecisions.Application.AvailabilityEngine;
using PourDecisions.Core.Entities;
using PourDecisions.Core.Enums;
using PourDecisions.Desktop.ViewModels;

namespace PourDecisions.UnitTests.ViewModels;

public class CocktailSummaryViewModelTests
{
    [Fact]
    public void Properties_Match_CocktailAndAvailability()
    {
        // Arrange
        var cocktail = new Cocktail
        {
            Id = 1,
            Name = "Martini",
            Instructions = "Stir",
            IsFavorite = true,
            CocktailIngredients = new List<CocktailIngredient>
            {
                new()
                {
                    Type = new IngredientType { Name = "Gin" }, AmountValue = 60, AmountUnit = AmountUnit.Ml
                }
            }
        };
        var availability = new AvailabilityResult(AvailabilityStatus.Available, new List<CocktailIngredient>());

        // Act
        var viewModel = new CocktailSummaryViewModel(cocktail, availability);

        // Assert
        Assert.Equal("Martini", viewModel.Name);
        Assert.Equal("Stir", viewModel.Instructions);
        Assert.True(viewModel.IsFavorite);
        Assert.Equal(AvailabilityStatus.Available, viewModel.AvailabilityStatus);
        Assert.Contains("available", viewModel.AvailabilityLabel);
    }

    [Fact]
    public void AvailabilityLabel_ShowsMissingCount_WhenUnavailable()
    {
        // Arrange
        var cocktail = new Cocktail { Id = 1, CocktailIngredients = new List<CocktailIngredient>() };
        var missingIngredient = new CocktailIngredient { TypeId = 1 };
        var availability = new AvailabilityResult(AvailabilityStatus.Unavailable,
            new List<CocktailIngredient> { missingIngredient });

        // Act
        var viewModel = new CocktailSummaryViewModel(cocktail, availability);

        // Assert
        Assert.Equal(AvailabilityStatus.Unavailable, viewModel.AvailabilityStatus);
        Assert.Contains("missing 1", viewModel.AvailabilityLabel);
    }

    [Fact]
    public void FavoriteToggled_InvokesEvent()
    {
        // Arrange
        var cocktail = new Cocktail
            { Id = 42, IsFavorite = false, CocktailIngredients = new List<CocktailIngredient>() };
        var availability = new AvailabilityResult(AvailabilityStatus.Available, new List<CocktailIngredient>());
        var viewModel = new CocktailSummaryViewModel(cocktail, availability);
        int? invokedId = null;
        bool? invokedFavorite = null;
        viewModel.FavoriteToggled += (id, favorite) =>
        {
            invokedId = id;
            invokedFavorite = favorite;
        };

        // Act
        viewModel.IsFavorite = true;

        // Assert
        Assert.Equal(42, invokedId);
        Assert.True(invokedFavorite);
    }

    [Fact]
    public void Ingredients_AreCorrectlyFormatted()
    {
        // Arrange
        var cocktail = new Cocktail
        {
            CocktailIngredients = new List<CocktailIngredient>
            {
                new()
                {
                    Type = new IngredientType { Name = "Gin" }, AmountValue = 60, AmountUnit = AmountUnit.Ml
                },
                new()
                {
                    Type = new IngredientType { Name = "Lemon" }, AmountValue = 1, AmountUnit = AmountUnit.Piece
                }
            }
        };
        var availability = new AvailabilityResult(AvailabilityStatus.Available, new List<CocktailIngredient>());

        // Act
        var viewModel = new CocktailSummaryViewModel(cocktail, availability);

        // Assert
        Assert.Equal(2, viewModel.IngredientInfo.Count);
        Assert.Equal("60 ml of gin", viewModel.IngredientInfo[0].DisplayText);
        Assert.False(viewModel.IngredientInfo[0].IsMissing);
        Assert.Equal("1 piece of lemon", viewModel.IngredientInfo[1].DisplayText);
        Assert.False(viewModel.IngredientInfo[1].IsMissing);
    }

    [Fact]
    public void Ingredients_FlagMissingStatus()
    {
        // Arrange
        var ci1 = new CocktailIngredient
        {
            TypeId = 1, Type = new IngredientType { Name = "Gin" }, AmountValue = 60, AmountUnit = AmountUnit.Ml
        };
        var ci2 = new CocktailIngredient
        {
            TypeId = 2, Type = new IngredientType { Name = "Vermouth" }, AmountValue = 15, AmountUnit = AmountUnit.Ml
        };

        var cocktail = new Cocktail
        {
            CocktailIngredients = new List<CocktailIngredient> { ci1, ci2 }
        };
        var availability = new AvailabilityResult(AvailabilityStatus.Unavailable, new List<CocktailIngredient> { ci2 });

        // Act
        var viewModel = new CocktailSummaryViewModel(cocktail, availability);

        // Assert
        Assert.False(viewModel.IngredientInfo[0].IsMissing);
        Assert.True(viewModel.IngredientInfo[1].IsMissing);
    }
}
