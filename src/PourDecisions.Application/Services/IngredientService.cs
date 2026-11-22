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
    Task<RenameIngredientResult> RenameIngredientAsync(int id, string newName);

    /// <summary>
    /// Asynchronously merges two ingredient types into one.
    /// </summary>
    /// <param name="sourceId">The ID of the ingredient type to be merged.</param>
    /// <param name="targetId">The ID of the ingredient type to merge into.</param>
    Task<MergeIngredientResult> MergeIngredientTypesAsync(int sourceId, int targetId);

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
    public async Task<RenameIngredientResult> RenameIngredientAsync(int id, string newName)
    {
        var normalizedName = newName.ToTitleCase();

        if (normalizedName.Length < 3)
        {
            return new RenameIngredientResult.Error("Ingredient type name must be at least 3 characters long.");
        }

        var ingredientType = await cocktailDbContext.IngredientTypes.FirstOrDefaultAsync(type => type.Id == id);

        if (ingredientType is null)
        {
            return new RenameIngredientResult.Error($"Ingredient type with ID {id} not found.");
        }

        if (normalizedName.Equals(ingredientType.Name, StringComparison.OrdinalIgnoreCase))
        {
            return new RenameIngredientResult.Error("Enter a new ingredient type name.");
        }

        var existingCocktail = await cocktailDbContext.IngredientTypes
            .FirstOrDefaultAsync(type => type.Id != id && type.Name == normalizedName);

        if (existingCocktail is not null)
        {
            var cocktailsBlockingMerge = await CocktailsBlockingMergeAsync(id, existingCocktail.Id);

            return cocktailsBlockingMerge.Count > 0
                ? new RenameIngredientResult.NameCollisionMergeBlocked(cocktailsBlockingMerge)
                : new RenameIngredientResult.NameCollisionCanMerge(normalizedName, existingCocktail.Id);
        }

        ingredientType.Name = normalizedName;
        await cocktailDbContext.SaveChangesAsync();
        return new RenameIngredientResult.Success(normalizedName);
    }

    /// <inheritdoc/>
    public async Task<MergeIngredientResult> MergeIngredientTypesAsync(int sourceId, int targetId)
    {
        if (sourceId == targetId)
        {
            return new MergeIngredientResult.Error("Cannot merge an ingredient type into itself.");
        }

        var sourceType = await cocktailDbContext.IngredientTypes.FirstOrDefaultAsync(type => type.Id == sourceId);
        var targetType = await cocktailDbContext.IngredientTypes.FirstOrDefaultAsync(type => type.Id == targetId);

        if (sourceType is null || targetType is null)
        {
            return new MergeIngredientResult.Error("One or both ingredient types not found.");
        }

        var cocktailsWithBothTypes = await CocktailsBlockingMergeAsync(sourceId, targetId);

        if (cocktailsWithBothTypes.Count > 0)
        {
            return new MergeIngredientResult.RecipeConflict(cocktailsWithBothTypes);
        }

        var bottlesToUpdate = await cocktailDbContext.Bottles.Where(b => b.TypeId == sourceId).ToListAsync();
        foreach (var bottle in bottlesToUpdate)
        {
            bottle.TypeId = targetId;
        }

        if (sourceType.IsTracked && bottlesToUpdate.Count > 0)
        {
            targetType.IsTracked = true;
        }

        var cocktailIngredientsToUpdate =
            await cocktailDbContext.CocktailIngredients.Where(ci => ci.TypeId == sourceId).ToListAsync();
        foreach (var ci in cocktailIngredientsToUpdate)
        {
            ci.TypeId = targetId;
        }

        cocktailDbContext.IngredientTypes.Remove(sourceType);

        await cocktailDbContext.SaveChangesAsync();

        return new MergeIngredientResult.Success(targetType.IsTracked);
    }

    /// <inheritdoc/>
    public async Task<IngredientTypeModifyImpact> PreviewIngredientTypeModifyAsync(int id)
    {
        var cocktailNames = await cocktailDbContext.Cocktails
            .Where(cocktail => cocktail.CocktailIngredients.Any(i => i.TypeId == id))
            .Select(cocktail => cocktail.Name)
            .ToListAsync();

        var bottleSummaries = await cocktailDbContext.Bottles
            .Where(bottle => bottle.TypeId == id)
            .Select(bottle => new BottleSummary(bottle.Name, bottle.Volume))
            .ToListAsync();

        return new IngredientTypeModifyImpact { CocktailNames = cocktailNames, BottleInfoList = bottleSummaries };
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

    private async Task<List<string>> CocktailsBlockingMergeAsync(int sourceId, int targetId)
    {
        return await cocktailDbContext.Cocktails
            .Where(cocktail => cocktail.CocktailIngredients.Any(ingredient => ingredient.TypeId == sourceId)
                               && cocktail.CocktailIngredients.Any(ingredient => ingredient.TypeId == targetId))
            .Select(cocktail => cocktail.Name)
            .ToListAsync();
    }
}
