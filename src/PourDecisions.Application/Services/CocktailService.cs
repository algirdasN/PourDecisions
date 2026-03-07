using Microsoft.EntityFrameworkCore;
using PourDecisions.Core.Data;
using PourDecisions.Core.Entities;

namespace PourDecisions.Application.Services;

public interface ICocktailService
{
    Task<List<Cocktail>> GetAllWithIngredientsAsync();
    Task SetFavoriteAsync(int cocktailId, bool isFavorite);
}

public class CocktailService(CocktailDbContext cocktailDbContext) : ICocktailService
{
    public async Task<List<Cocktail>> GetAllWithIngredientsAsync()
    {
        return await cocktailDbContext.Cocktails
            .Include(c => c.CocktailIngredients)
            .ThenInclude(ci => ci.Type)
            .ToListAsync();
    }

    public async Task SetFavoriteAsync(int cocktailId, bool isFavorite)
    {
        var cocktail = await cocktailDbContext.Cocktails
            .SingleAsync(cocktail => cocktail.Id == cocktailId);

        cocktail.IsFavorite = isFavorite;
        await cocktailDbContext.SaveChangesAsync();
    }
}
