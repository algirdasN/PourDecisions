using Microsoft.EntityFrameworkCore;
using PourDecisions.Core.Data;

namespace PourDecisions.Application.AvailabilityEngine;

public interface IAvailabilityService
{
    Task<Dictionary<int, AvailabilityResult>> GetCocktailAvailabilityAsync();
}

public class AvailabilityService(CocktailDbContext cocktailDbContext) : IAvailabilityService
{
    public async Task<Dictionary<int, AvailabilityResult>> GetCocktailAvailabilityAsync()
    {
        var availableTypeIds = cocktailDbContext.IngredientTypes
            .Where(type => !type.IsTracked || type.Bottles.Count > 0)
            .Select(type => type.Id)
            .ToHashSet();

        return await cocktailDbContext.Cocktails
            .Include(x => x.CocktailIngredients)
            .ToDictionaryAsync(
                cocktail => cocktail.Id,
                cocktail => AvailabilityCalculator.CalculateCocktailAvailability(cocktail, availableTypeIds));
    }
}
