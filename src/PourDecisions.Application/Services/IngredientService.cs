using Microsoft.EntityFrameworkCore;
using PourDecisions.Core.Data;

namespace PourDecisions.Application.Services;

public interface IIngredientService
{
    Task<List<string>> GetTrackedIngredientTypeNamesAsync();
}

public class IngredientService(CocktailDbContext cocktailDbContext) : IIngredientService
{
    public async Task<List<string>> GetTrackedIngredientTypeNamesAsync()
    {
        return await cocktailDbContext.IngredientTypes
            .Where(type => type.IsTracked)
            .Select(type => type.Name)
            .OrderBy(name => name)
            .ToListAsync();
    }
}
