using Microsoft.EntityFrameworkCore;
using PourDecisions.Core.Data;
using PourDecisions.Core.Entities;

namespace PourDecisions.Application.Services;

/// <summary>
/// Provides an interface for managing ingredient type operations.
/// </summary>
public interface IIngredientService
{
    /// <summary>
    /// Asynchronously retrieves all ingredient types along with their associated bottles in alphabetical order.
    /// </summary>
    /// <returns>A sorted list of all ingredients with bottles.</returns>
    Task<List<IngredientType>> GetIngredientTypesAsync();

    /// <summary>
    /// Asynchronously retrieves the names of all ingredient types in alphabetical order.
    /// </summary>
    /// <returns>A sorted list of all ingredient type names.</returns>
    Task<List<string>> GetIngredientTypeNamesAsync();

    /// <summary>
    /// Asynchronously retrieves the names of all tracked ingredient types in alphabetical order.
    /// </summary>
    /// <returns>A sorted list of tracked ingredient type names.</returns>
    Task<List<string>> GetTrackedIngredientTypeNamesAsync();

    /// <summary>
    /// Asynchronously deletes an ingredient type by its ID.
    /// </summary>
    /// <param name="id">The ID of the ingredient type to delete.</param>
    Task DeleteIngredientTypeAsync(int id);
}

/// <summary>
/// Implements ingredient type operations using Entity Framework Core.
/// </summary>
/// <remarks>
/// This service provides access to ingredient type information, particularly
/// for tracked ingredients that have inventory management enabled.
/// </remarks>
public class IngredientService(CocktailDbContext cocktailDbContext) : IIngredientService
{
    /// <inheritdoc/>
    public async Task<List<IngredientType>> GetIngredientTypesAsync()
    {
        return await cocktailDbContext.IngredientTypes
            .OrderBy(type => type.Name)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetIngredientTypeNamesAsync()
    {
        return await cocktailDbContext.IngredientTypes
            .Select(type => type.Name)
            .OrderBy(name => name)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetTrackedIngredientTypeNamesAsync()
    {
        return await cocktailDbContext.IngredientTypes
            .Where(type => type.IsTracked)
            .Select(type => type.Name)
            .OrderBy(name => name)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task DeleteIngredientTypeAsync(int id)
    {
        var ingredientType = await cocktailDbContext.IngredientTypes
            .FirstOrDefaultAsync(type => type.Id == id);

        if (ingredientType != null)
        {
            cocktailDbContext.IngredientTypes.Remove(ingredientType);
            await cocktailDbContext.SaveChangesAsync();
        }
    }
}
