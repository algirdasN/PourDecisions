using Microsoft.EntityFrameworkCore;
using PourDecisions.Core.Data;

namespace PourDecisions.Application.AvailabilityEngine;

/// <summary>
/// Provides an interface for retrieving cocktail availability information.
/// </summary>
public interface IAvailabilityService
{
    /// <summary>
    /// Asynchronously retrieves the availability status for all cocktails in the system.
    /// </summary>
    /// <returns> A dictionary where the key is the cocktail ID and the value is an <see cref="AvailabilityResult"/>.</returns>
    Task<Dictionary<int, AvailabilityResult>> GetCocktailAvailabilityAsync();
}

/// <summary>
/// Implements availability calculations for cocktails using the Entity Framework Core context.
/// </summary>
/// <remarks>
/// This service determines which cocktails can be made based on available ingredient types.
/// Ingredient types are considered available if they are either not tracked or have at least one bottle in stock.
/// </remarks>
public class AvailabilityService(CocktailDbContext cocktailDbContext) : IAvailabilityService
{
    /// <inheritdoc/>
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
