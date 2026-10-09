using Microsoft.EntityFrameworkCore;
using PourDecisions.Application.Models;
using PourDecisions.Core.Data;
using PourDecisions.Core.Entities;
using PourDecisions.Shared.Extensions;

namespace PourDecisions.Application.Services;

/// <summary>
/// Provides an interface for managing ingredient type operations.
/// </summary>
public interface IIngredientService
{
    /// <summary>
    /// Asynchronously retrieves all ingredient types in alphabetical order.
    /// </summary>
    /// <returns>A sorted list of all ingredients.</returns>
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
    /// Asynchronously previews the impact of modifying an ingredient type.
    /// Retrieves a list of cocktails and bottles associated with the specified ingredient type.
    /// </summary>
    /// <param name="id">The unique identifier of the ingredient type to preview modification for.</param>
    /// <returns>A data object containing the names of cocktails using the ingredient type and a list of associated bottles, if any.</returns>
    Task<IngredientTypeModifyImpact> PreviewIngredientTypeModifyAsync(int id);

    /// <summary>
    /// Asynchronously updates the tracked status of an ingredient type and performs cleanup of associated bottles if necessary.
    /// </summary>
    /// <param name="id">The unique identifier of the ingredient type to update.</param>
    /// <param name="newValue">The new tracked status value to set for the ingredient type.</param>
    /// <param name="allowWithBottles">Whether to allow updating of ingredient types that have associated bottles.</param>
    Task UpdateIngredientTypeTrackedAsync(int id, bool newValue, bool allowWithBottles = false);

    /// <summary>
    /// Asynchronously deletes an ingredient type by its ID.
    /// </summary>
    /// <param name="id">The ID of the ingredient type to delete.</param>
    /// <param name="allowWithBottles">Whether to allow deletion of ingredient types that have associated bottles.</param>
    Task DeleteIngredientTypeAsync(int id, bool allowWithBottles = false);
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
        var ingredientType = await cocktailDbContext.IngredientTypes.FirstOrDefaultAsync(type => type.Id == id);

        if (ingredientType is null)
        {
            return;
        }

        var normalizedName = newName.ToTitleCase();

        var nameExists = await cocktailDbContext.IngredientTypes
            .AnyAsync(type => type.Id != id && type.Name == normalizedName);

        if (nameExists)
        {
            throw new InvalidOperationException($"Ingredient type '{normalizedName}' already exists.");
        }

        ingredientType.Name = normalizedName;
        await cocktailDbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<IngredientTypeModifyImpact> PreviewIngredientTypeModifyAsync(int id)
    {
        var cocktailsWithIngredient = await cocktailDbContext.Cocktails
            .Where(cocktail => cocktail.CocktailIngredients.Any(i => i.TypeId == id))
            .ToListAsync();

        var bottlesWithIngredient = await cocktailDbContext.Bottles
            .Where(bottle => bottle.TypeId == id)
            .ToListAsync();

        return new IngredientTypeModifyImpact
        {
            CocktailNames = cocktailsWithIngredient.Select(c => c.Name).ToList(),
            BottleInfoList = bottlesWithIngredient.Select(b => new BottleSummary(b.Name, b.Volume)).ToList()
        };
    }

    /// <inheritdoc/>
    public async Task UpdateIngredientTypeTrackedAsync(int id, bool newValue, bool allowWithBottles = false)
    {
        var deleteBottles = false;
        if (!newValue)
        {
            var modifyImpact = await PreviewIngredientTypeModifyAsync(id);

            if (modifyImpact.BottleInfoList.Count > 0 && !allowWithBottles)
            {
                throw new InvalidOperationException(
                    $"Cannot update ingredient type with ID {id} because it is used in bottles: {string.Join(", ", modifyImpact.BottleInfoList)}");
            }

            deleteBottles = modifyImpact.BottleInfoList.Count > 0;
        }

        var ingredientType = await cocktailDbContext.IngredientTypes.FirstOrDefaultAsync(type => type.Id == id);

        if (ingredientType is not null)
        {
            ingredientType.IsTracked = newValue;

            if (deleteBottles)
            {
                var bottles = await cocktailDbContext.Bottles.Where(b => b.TypeId == id).ToListAsync();
                cocktailDbContext.Bottles.RemoveRange(bottles);
            }

            await cocktailDbContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc/>
    public async Task DeleteIngredientTypeAsync(int id, bool allowWithBottles = false)
    {
        var modifyImpact = await PreviewIngredientTypeModifyAsync(id);

        if (modifyImpact.CocktailNames.Count != 0)
        {
            throw new InvalidOperationException(
                $"Cannot delete ingredient type with ID {id} because it is used in cocktails: {string.Join(", ", modifyImpact.CocktailNames)}");
        }

        if (modifyImpact.BottleInfoList.Count != 0 && !allowWithBottles)
        {
            throw new InvalidOperationException(
                $"Cannot delete ingredient type with ID {id} because it is used in bottles: {string.Join(", ", modifyImpact.BottleInfoList)}");
        }

        var ingredientType = await cocktailDbContext.IngredientTypes.FirstOrDefaultAsync(type => type.Id == id);

        if (ingredientType is not null)
        {
            cocktailDbContext.IngredientTypes.Remove(ingredientType);
            await cocktailDbContext.SaveChangesAsync();
        }
    }
}
