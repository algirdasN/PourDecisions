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
                new() { TypeId = 1 },
                new() { TypeId = 2 }
            }
        };
        var availableIngredients = new HashSet<int> { 1, 2, 3 };

        // Act
        var result = AvailabilityCalculator.CalculateCocktailAvailability(cocktail, availableIngredients);

        // Assert
        Assert.Equal(AvailabilityStatus.Available, result.Status);
        Assert.Empty(result.MissingIngredients);
    }

    [Fact]
    public void CalculateCocktailAvailability_MissingRequiredIngredient_ReturnsUnavailable()
    {
        // Arrange
        var cocktail = new Cocktail
        {
            CocktailIngredients = new List<CocktailIngredient>
            {
                new() { TypeId = 1 },
                new() { TypeId = 2 }
            }
        };
        var availableIngredients = new HashSet<int> { 1, 3 };

        // Act
        var result = AvailabilityCalculator.CalculateCocktailAvailability(cocktail, availableIngredients);

        // Assert
        Assert.Equal(AvailabilityStatus.Unavailable, result.Status);
        Assert.Single(result.MissingIngredients, cocktailIngredient => cocktailIngredient.TypeId == 2);
    }
}
