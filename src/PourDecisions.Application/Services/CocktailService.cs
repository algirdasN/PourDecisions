using Microsoft.EntityFrameworkCore;
using PourDecisions.Application.Models;
using PourDecisions.Core.Data;
using PourDecisions.Core.Entities;
using PourDecisions.Shared.Extensions;

namespace PourDecisions.Application.Services;

/// <summary>
/// Provides an interface for managing cocktail operations, including retrieval and favorite status updates.
/// </summary>
public interface ICocktailService
{
    /// <summary>
    /// Asynchronously retrieves a single cocktail with its associated ingredients and ingredient types.
    /// </summary>
    /// <param name="cocktailId">The ID of the cocktail to retrieve.</param>
    /// <returns>The <see cref="Cocktail"/> entity with its <see cref="CocktailIngredient"/> and <see cref="IngredientType"/> data loaded.</returns>
    Task<Cocktail> GetWithIngredientsAsync(int cocktailId);

    /// <summary>
    /// Asynchronously retrieves all cocktails with their associated ingredients and ingredient types.
    /// </summary>
    /// <returns>A list of all <see cref="Cocktail"/> entities with their <see cref="CocktailIngredient"/> and <see cref="IngredientType"/> data loaded.</returns>
    Task<List<Cocktail>> GetAllWithIngredientsAsync();

    /// <summary>
    /// Asynchronously retrieves a summary list of all cocktails.
    /// </summary>
    /// <returns>A list of all <see cref="CocktailEditSummary"/> objects.</returns>
    Task<List<CocktailEditSummary>> GetAllSummariesAsync();

    /// <summary>
    /// Asynchronously adds a new cocktail with its ingredients and instructions.
    /// </summary>
    /// <param name="name">The name of the cocktail.</param>
    /// <param name="ingredientSummaries">A list of ingredient summaries for the cocktail.</param>
    /// <param name="instructions">The preparation instructions for the cocktail.</param>
    /// <returns>The ID of the newly created cocktail.</returns>
    Task<int> AddCocktailAsync(string name, IList<CocktailIngredientSummary> ingredientSummaries, string instructions);

    /// <summary>
    /// Asynchronously updates an existing cocktail.
    /// </summary>
    /// <param name="id">The ID of the cocktail to update.</param>
    /// <param name="name">The new name for the cocktail.</param>
    /// <param name="ingredientSummaries">The new list of ingredient summaries for the cocktail.</param>
    /// <param name="instructions">The new preparation instructions for the cocktail.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EditCocktailAsync(int id, string name, IList<CocktailIngredientSummary> ingredientSummaries,
        string instructions);

    /// <summary>
    /// Asynchronously updates the favorite status of a cocktail.
    /// </summary>
    /// <param name="cocktailId">The ID of the cocktail to update.</param>
    /// <param name="isFavorite">Whether the cocktail should be marked as a favorite.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetFavoriteAsync(int cocktailId, bool isFavorite);

    /// <summary>
    /// Asynchronously deletes a cocktail by its ID.
    /// </summary>
    /// <param name="cocktailId">The ID of the cocktail to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteCocktailAsync(int cocktailId);
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
    public async Task<Cocktail> GetWithIngredientsAsync(int cocktailId)
    {
        return await cocktailDbContext.Cocktails
            .Include(c => c.CocktailIngredients.OrderBy(ci => ci.SortOrder))
            .ThenInclude(ci => ci.Type)
            .FirstAsync(c => c.Id == cocktailId);
    }

    /// <inheritdoc/>
    public async Task<List<Cocktail>> GetAllWithIngredientsAsync()
    {
        return await cocktailDbContext.Cocktails
            .Include(c => c.CocktailIngredients.OrderBy(ci => ci.SortOrder))
            .ThenInclude(ci => ci.Type)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<CocktailEditSummary>> GetAllSummariesAsync()
    {
        return await cocktailDbContext.Cocktails
            .OrderBy(c => c.Name)
            .Select(c => new CocktailEditSummary(c.Id, c.Name, c.IsFavorite))
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<int> AddCocktailAsync(string name, IList<CocktailIngredientSummary> ingredientSummaries,
        string instructions)
    {
        var newCocktail = new Cocktail
        {
            Name = name.ToTitleCase(),
            Instructions = instructions,
            CocktailIngredients = await BuildCocktailIngredients(ingredientSummaries)
        };

        cocktailDbContext.Cocktails.Add(newCocktail);
        await cocktailDbContext.SaveChangesAsync();

        return newCocktail.Id;
    }

    /// <inheritdoc/>
    public async Task EditCocktailAsync(int id, string name, IList<CocktailIngredientSummary> ingredientSummaries,
        string instructions)
    {
        var cocktail = await cocktailDbContext.Cocktails
            .Include(cocktail => cocktail.CocktailIngredients)
            .FirstAsync(c => c.Id == id);

        cocktail.CocktailIngredients.Clear();

        cocktail.Name = name.ToTitleCase();
        cocktail.Instructions = instructions;
        cocktail.CocktailIngredients = await BuildCocktailIngredients(ingredientSummaries);

        await cocktailDbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task SetFavoriteAsync(int cocktailId, bool isFavorite)
    {
        var cocktail = await cocktailDbContext.Cocktails
            .FirstAsync(c => c.Id == cocktailId);

        cocktail.IsFavorite = isFavorite;
        await cocktailDbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task DeleteCocktailAsync(int cocktailId)
    {
        var cocktail = await cocktailDbContext.Cocktails
            .FirstAsync(c => c.Id == cocktailId);

        cocktailDbContext.Cocktails.Remove(cocktail);
        await cocktailDbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Builds a list of <see cref="CocktailIngredient"/> entities from a list of ingredient summaries.
    /// </summary>
    /// <param name="ingredientSummaries">The summaries to build ingredients from.</param>
    /// <returns>A list of <see cref="CocktailIngredient"/> entities.</returns>
    private async Task<List<CocktailIngredient>> BuildCocktailIngredients(
        IList<CocktailIngredientSummary> ingredientSummaries)
    {
        var summaryNames = ingredientSummaries.Select(s => s.Name.ToLower()).Distinct().ToList();

        var typeCache = await cocktailDbContext.IngredientTypes
            .Where(t => summaryNames.Contains(t.Name.ToLower()))
            .ToDictionaryAsync(t => t.Name.ToLower());

        return ingredientSummaries
            .Select((summary, index) =>
            {
                var lowerName = summary.Name.Trim().ToLower();

                if (!typeCache.TryGetValue(lowerName, out var ingredientType))
                {
                    ingredientType = new IngredientType { Name = summary.Name.ToTitleCase(), IsTracked = false };
                    typeCache[lowerName] = ingredientType;
                }

                return new CocktailIngredient
                {
                    SortOrder = index,
                    AmountValue = summary.Amount,
                    AmountUnit = summary.Unit,
                    Type = ingredientType
                };
            })
            .ToList();
    }
}
