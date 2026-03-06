using PourDecisions.Application.AvailabilityEngine;
using PourDecisions.Core.Entities;

namespace PourDecisions.UnitTests.AvailabilityEngine;

public class AvailabilityCalculatorTests
{
    [Fact]
    public void CalculateCocktailAvailability_AllIngredientsAvailable_ReturnsAvailable()
    {
        // Arrange
        var cocktail = new Cocktail
            {
                CocktailIngredients = new List<CocktailIngredient>
                {
                    new() { TypeId = 1, IsOptional = false },
                    new() { TypeId = 2, IsOptional = true }
                }
            }
            ;
        var availableIngredients = new HashSet<int> { 1, 2, 3 };

        // Act
        var result = AvailabilityCalculator.CalculateCocktailAvailability(cocktail, availableIngredients);

        // Assert
        Assert.Equal(AvailabilityStatus.Available, result.Status);
        Assert.Empty(result.MissingRequired);
        Assert.Empty(result.MissingOptional);
    }

    [Fact]
    public void CalculateCocktailAvailability_MissingRequiredIngredient_ReturnsUnavailable()
    {
        // Arrange
        var cocktail = new Cocktail
        {
            CocktailIngredients = new List<CocktailIngredient>
            {
                new() { TypeId = 1, IsOptional = false },
                new() { TypeId = 2, IsOptional = false }
            }
        };
        var availableIngredients = new HashSet<int> { 1, 3 };

        // Act
        var result = AvailabilityCalculator.CalculateCocktailAvailability(cocktail, availableIngredients);

        // Assert
        Assert.Equal(AvailabilityStatus.Unavailable, result.Status);
        Assert.Single(result.MissingRequired, cocktailIngredient => cocktailIngredient.TypeId == 2);
        Assert.Empty(result.MissingOptional);
    }

    [Fact]
    public void CalculateCocktailAvailability_MissingOptionalIngredient_ReturnsAvailable()
    {
        // Arrange
        var cocktail = new Cocktail
        {
            CocktailIngredients = new List<CocktailIngredient>
            {
                new() { TypeId = 1, IsOptional = false },
                new() { TypeId = 2, IsOptional = true }
            }
        };
        var availableIngredients = new HashSet<int> { 1, 3 };

        // Act
        var result = AvailabilityCalculator.CalculateCocktailAvailability(cocktail, availableIngredients);

        // Assert
        Assert.Equal(AvailabilityStatus.Available, result.Status);
        Assert.Empty(result.MissingRequired);
        Assert.Single(result.MissingOptional, cocktailIngredient => cocktailIngredient.TypeId == 2);
    }

    [Fact]
    public void CalculateCocktailAvailability_MissingBothRequiredAndOptionalIngredients_ReturnsUnavailable()
    {
        // Arrange
        var cocktail = new Cocktail
        {
            CocktailIngredients = new List<CocktailIngredient>
            {
                new() { TypeId = 1, IsOptional = false },
                new() { TypeId = 2, IsOptional = true },
                new() { TypeId = 4, IsOptional = false }
            }
        };
        var availableIngredients = new HashSet<int> { 3 };

        // Act
        var result = AvailabilityCalculator.CalculateCocktailAvailability(cocktail, availableIngredients);

        // Assert
        Assert.Equal(AvailabilityStatus.Unavailable, result.Status);
        Assert.Collection(result.MissingRequired,
            cocktailIngredient => Assert.Equal(1, cocktailIngredient.TypeId),
            cocktailIngredient => Assert.Equal(4, cocktailIngredient.TypeId));
        Assert.Single(result.MissingOptional, i => i.TypeId == 2);
    }
}