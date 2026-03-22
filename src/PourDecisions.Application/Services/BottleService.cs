using Microsoft.EntityFrameworkCore;
using PourDecisions.Core.Data;
using PourDecisions.Core.Entities;
using PourDecisions.Core.Enums;
using PourDecisions.Shared.Extensions;

namespace PourDecisions.Application.Services;

/// <summary>
/// Provides an interface for managing bottle operations including creation, retrieval, updates, and deletion.
/// </summary>
public interface IBottleService
{
    /// <summary>
    /// Asynchronously adds a new bottle with the specified ingredient type.
    /// </summary>
    /// <remarks>
    /// If the ingredient type does not exist, it will be created. The ingredient type will be marked as tracked.
    /// </remarks>
    /// <param name="typeName">The name of the ingredient type.</param>
    /// <param name="bottleName">The name of the bottle.</param>
    /// <param name="volume">The volume of the bottle in milliliters.</param>
    /// <param name="fillLevel">The current fill level of the bottle.</param>
    /// <returns>The newly created <see cref="Bottle"/> entity.</returns>
    Task<Bottle> AddBottleAsync(string typeName, string bottleName, int volume, FillLevel fillLevel);

    /// <summary>
    /// Asynchronously retrieves all bottles with their associated ingredient type information.
    /// </summary>
    /// <returns>A list of all <see cref="Bottle"/> entities with their <see cref="IngredientType"/> data loaded.</returns>
    Task<List<Bottle>> GetBottlesWithTypeAsync();

    /// <summary>
    /// Asynchronously retrieves all bottles of a specific ingredient type.
    /// </summary>
    /// <param name="typeId">The ID of the ingredient type.</param>
    /// <returns>A list of <see cref="Bottle"/> entities that belong to the specified ingredient type.</returns>
    Task<List<Bottle>> GetBottlesOfTypeAsync(int typeId);

    /// <summary>
    /// Asynchronously updates the fill level of a bottle.
    /// </summary>
    /// <param name="bottleId">The ID of the bottle to update.</param>
    /// <param name="newFill">The new fill level for the bottle.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateBottleFillLevelAsync(int bottleId, FillLevel newFill);

    /// <summary>
    /// Asynchronously deletes a bottle by its ID.
    /// </summary>
    /// <param name="bottleId">The ID of the bottle to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteBottleAsync(int bottleId);
}

/// <summary>
/// Implements bottle management operations using Entity Framework Core.
/// </summary>
/// <remarks>
/// This service handles the creation, retrieval, updating, and deletion of bottles,
/// including automatic management of associated ingredient types.
/// </remarks>
public class BottleService(CocktailDbContext cocktailDbContext) : IBottleService
{
    /// <inheritdoc/>
    public async Task<Bottle> AddBottleAsync(string typeName, string bottleName, int volume, FillLevel fillLevel)
    {
        var ingredientType = await cocktailDbContext.IngredientTypes
            .FirstOrDefaultAsync(type => type.Name.ToLower() == typeName.ToLower());

        if (ingredientType is null)
        {
            ingredientType = new IngredientType { Name = typeName.ToTitleCase(), IsTracked = true };
        }
        else
        {
            ingredientType.IsTracked = true;
        }

        var bottle = new Bottle { Name = bottleName, Volume = volume, FillLevel = fillLevel, Type = ingredientType };

        await cocktailDbContext.Bottles.AddAsync(bottle);
        await cocktailDbContext.SaveChangesAsync();

        return bottle;
    }

    /// <inheritdoc/>
    public async Task<List<Bottle>> GetBottlesWithTypeAsync()
    {
        return await cocktailDbContext.Bottles
            .Include(bottles => bottles.Type)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<Bottle>> GetBottlesOfTypeAsync(int typeId)
    {
        return await cocktailDbContext.Bottles
            .Where(bottles => bottles.TypeId == typeId)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task DeleteBottleAsync(int bottleId)
    {
        var bottle = await cocktailDbContext.Bottles
            .FirstOrDefaultAsync(b => b.Id == bottleId);

        if (bottle != null)
        {
            cocktailDbContext.Bottles.Remove(bottle);
            await cocktailDbContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc/>
    public async Task UpdateBottleFillLevelAsync(int bottleId, FillLevel newFill)
    {
        var bottle = await cocktailDbContext.Bottles
            .SingleAsync(bottles => bottles.Id == bottleId);

        bottle.FillLevel = newFill;
        await cocktailDbContext.SaveChangesAsync();
    }
}
