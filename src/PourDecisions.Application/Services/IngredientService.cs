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
    /// Asynchronously renames an ingredient type.
    /// </summary>
    /// <param name="id">The ID of the ingredient type to rename</param>
    /// <param name="newName">New name of the ingredient type.</param>
    Task RenameIngredientAsync(int id, string newName);

    /// <summary>
    /// Asynchronously updates the tracked status of an ingredient type and performs cleanup of associated bottles if necessary.
    /// </summary>
    /// <param name="id">The unique identifier of the ingredient type to update.</param>
    /// <param name="newValue">The new tracked status value to set for the ingredient type.</param>
    Task UpdateIngredientTypeTrackedWithBottleCleanupAsync(int id, bool newValue);

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
    public async Task RenameIngredientAsync(int id, string newName)
    {
        var ingredientType = await cocktailDbContext.IngredientTypes
            .FirstOrDefaultAsync(type => type.Id == id);

        if (ingredientType != null)
        {
            ingredientType.Name = newName;
            await cocktailDbContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc/>
    public async Task UpdateIngredientTypeTrackedWithBottleCleanupAsync(int id, bool newValue)
    {
        var ingredientType = await cocktailDbContext.IngredientTypes
            .FirstOrDefaultAsync(type => type.Id == id);

        if (ingredientType != null)
        {
            ingredientType.IsTracked = newValue;

            if (!newValue)
            {
                var bottles = await cocktailDbContext.Bottles.Where(b => b.TypeId == id).ToListAsync();
                cocktailDbContext.Bottles.RemoveRange(bottles);
            }

            await cocktailDbContext.SaveChangesAsync();
        }
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
