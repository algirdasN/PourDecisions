using Microsoft.EntityFrameworkCore;
using PourDecisions.Core.Data;

namespace PourDecisions.Application.AvailabilityEngine;

public interface IAvailabilityService
{
    IDictionary<int, AvailabilityResult> GetCocktailAvailability();
}

public class AvailabilityService(CocktailDbContext cocktailDbContext) : IAvailabilityService
{
    public IDictionary<int, AvailabilityResult> GetCocktailAvailability()
    {
        var availableTypeIds = cocktailDbContext.IngredientTypes
            .Where(type => type.IsTracked && type.Ingredients.Any(ingredient => ingredient.Bottles.Count > 0))
            .Select(type => type.Id)
            .ToHashSet();

        return cocktailDbContext.Cocktails
            .Include(x => x.CocktailIngredients)
            .ToDictionary(
                cocktail => cocktail.Id,
                cocktail => AvailabilityCalculator.CalculateCocktailAvailability(cocktail, availableTypeIds));
    }
}