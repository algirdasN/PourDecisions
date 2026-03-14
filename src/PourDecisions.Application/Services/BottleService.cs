using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PourDecisions.Core.Data;
using PourDecisions.Core.Entities;
using PourDecisions.Core.Enums;

namespace PourDecisions.Application.Services;

public interface IBottleService
{
    Task<Bottle> AddBottleAsync(string typeName, string bottleName, int volume, FillLevel fillLevel);
    Task<List<Bottle>> GetBottlesWithTypeAsync();
    Task<List<Bottle>> GetBottlesOfTypeAsync(int typeId);
    Task UpdateBottleFillLevelAsync(int bottleId, FillLevel newFill);
    Task DeleteBottleAsync(int bottleId);
}

public class BottleService(CocktailDbContext cocktailDbContext) : IBottleService
{
    public async Task<Bottle> AddBottleAsync(string typeName, string bottleName, int volume, FillLevel fillLevel)
    {
        var ingredientType = await cocktailDbContext.IngredientTypes
            .FirstOrDefaultAsync(type => type.Name.ToLower() == typeName.ToLower());

        if (ingredientType is null)
        {
            var capitalizedTypeName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(typeName.ToLower());

            ingredientType = new IngredientType { Name = capitalizedTypeName, IsTracked = true };
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

    public async Task<List<Bottle>> GetBottlesWithTypeAsync()
    {
        return await cocktailDbContext.Bottles
            .Include(bottles => bottles.Type)
            .ToListAsync();
    }

    public async Task<List<Bottle>> GetBottlesOfTypeAsync(int typeId)
    {
        return await cocktailDbContext.Bottles
            .Where(bottles => bottles.TypeId == typeId)
            .ToListAsync();
    }

    public async Task DeleteBottleAsync(int bottleId)
    {
        await cocktailDbContext.Bottles
            .Where(bottles => bottles.Id == bottleId)
            .ExecuteDeleteAsync();
    }

    public async Task UpdateBottleFillLevelAsync(int bottleId, FillLevel newFill)
    {
        var bottle = await cocktailDbContext.Bottles
            .SingleAsync(bottles => bottles.Id == bottleId);

        bottle.FillLevel = newFill;
        await cocktailDbContext.SaveChangesAsync();
    }
}
