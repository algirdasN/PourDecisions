using Microsoft.EntityFrameworkCore;
using PourDecisions.Core.Data;
using PourDecisions.Core.Entities;

namespace PourDecisions.Application.Services;

/// <summary>
/// Provides an interface for managing cocktail operations including retrieval and favorite status updates.
/// </summary>
public interface ICocktailService
{
    /// <summary>
    /// Asynchronously retrieves all cocktails with their associated ingredients and ingredient types.
    /// </summary>
    /// <returns>A list of all <see cref="Cocktail"/> entities with their <see cref="CocktailIngredient"/> and <see cref="IngredientType"/> data loaded.</returns>
    Task<List<Cocktail>> GetAllWithIngredientsAsync();

    /// <summary>
    /// Asynchronously updates the favorite status of a cocktail.
    /// </summary>
    /// <param name="cocktailId">The ID of the cocktail to update.</param>
    /// <param name="isFavorite">Whether the cocktail should be marked as a favorite.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetFavoriteAsync(int cocktailId, bool isFavorite);
}

/// <summary>
/// Implements cocktail management operations using Entity Framework Core.
/// </summary>
/// <remarks>
/// This service handles the retrieval of cocktails with their ingredient information,
/// and allows updating the favorite status of cocktails.
/// </remarks>
public class CocktailService(CocktailDbContext cocktailDbContext) : ICocktailService
{
    /// <inheritdoc/>
    public async Task<List<Cocktail>> GetAllWithIngredientsAsync()
    {
        return await cocktailDbContext.Cocktails
            .Include(c => c.CocktailIngredients)
            .ThenInclude(ci => ci.Type)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task SetFavoriteAsync(int cocktailId, bool isFavorite)
    {
        var cocktail = await cocktailDbContext.Cocktails
            .SingleAsync(cocktail => cocktail.Id == cocktailId);

        cocktail.IsFavorite = isFavorite;
        await cocktailDbContext.SaveChangesAsync();
    }
}
