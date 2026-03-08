using Microsoft.EntityFrameworkCore;
using PourDecisions.Application.Services;
using PourDecisions.Core.Data;
using PourDecisions.Core.Entities;
using PourDecisions.Core.Enums;

namespace PourDecisions.UnitTests.Services;

public class CocktailServiceTests
{
    private static DbContextOptions<CocktailDbContext> CreateInMemoryOptions(string databaseName)
    {
        return new DbContextOptionsBuilder<CocktailDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
    }

    [Fact]
    public async Task GetAllWithIngredientsAsync_ReturnsAllCocktails()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetAllWithIngredientsAsync_ReturnsAllCocktails));

        var ingredientType = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var cocktail1 = new Cocktail { Id = 1, Name = "Martini", Instructions = "Stir", CocktailIngredients = [] };
        var cocktail2 = new Cocktail { Id = 2, Name = "Negroni", Instructions = "Stir", CocktailIngredients = [] };

        await using (var context = new CocktailDbContext(options))
        {
            await context.AddRangeAsync(ingredientType, cocktail1, cocktail2);
            await context.SaveChangesAsync();
        }

        // Act
        List<Cocktail> result;
        await using (var context = new CocktailDbContext(options))
        {
            var service = new CocktailService(context);
            result = await service.GetAllWithIngredientsAsync();
        }

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c is { Id: 1, Name: "Martini" });
        Assert.Contains(result, c => c is { Id: 2, Name: "Negroni" });
    }

    [Fact]
    public async Task GetAllWithIngredientsAsync_EagerLoadsIngredients()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetAllWithIngredientsAsync_EagerLoadsIngredients));

        var ingredientType = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var cocktailIngredient = new CocktailIngredient
        {
            Id = 1,
            TypeId = 1,
            Type = ingredientType,
            AmountValue = 60,
            AmountUnit = AmountUnit.Ml,
            IsOptional = false
        };
        var cocktail = new Cocktail
        {
            Id = 1,
            Name = "Martini",
            Instructions = "Stir",
            CocktailIngredients = new List<CocktailIngredient> { cocktailIngredient }
        };

        await using (var context = new CocktailDbContext(options))
        {
            await context.AddRangeAsync(ingredientType, cocktail);
            await context.SaveChangesAsync();
        }

        // Act
        List<Cocktail> result;
        await using (var context = new CocktailDbContext(options))
        {
            var service = new CocktailService(context);
            result = await service.GetAllWithIngredientsAsync();
        }

        // Assert
        Assert.Single(result);
        var retrievedCocktail = result.First();
        Assert.Single(retrievedCocktail.CocktailIngredients);
        Assert.Equal("Gin", retrievedCocktail.CocktailIngredients.First().Type.Name);
    }

    [Fact]
    public async Task GetAllWithIngredientsAsync_WithEmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetAllWithIngredientsAsync_WithEmptyDatabase_ReturnsEmptyList));

        // Act
        await using var context = new CocktailDbContext(options);
        var service = new CocktailService(context);
        var result = await service.GetAllWithIngredientsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SetFavoriteAsync_SetsFavoriteToTrue()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(SetFavoriteAsync_SetsFavoriteToTrue));
        var cocktail = new Cocktail
            { Id = 1, Name = "Martini", Instructions = "Stir", IsFavorite = false, CocktailIngredients = [] };

        await using (var context = new CocktailDbContext(options))
        {
            await context.AddRangeAsync(cocktail);
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new CocktailDbContext(options))
        {
            var service = new CocktailService(context);
            await service.SetFavoriteAsync(1, true);
        }

        // Assert
        await using (var context = new CocktailDbContext(options))
        {
            var updatedCocktail = await context.Cocktails.SingleAsync(c => c.Id == 1);
            Assert.True(updatedCocktail.IsFavorite);
        }
    }

    [Fact]
    public async Task SetFavoriteAsync_SetsFavoriteToFalse()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(SetFavoriteAsync_SetsFavoriteToFalse));
        var cocktail = new Cocktail
            { Id = 1, Name = "Martini", Instructions = "Stir", IsFavorite = true, CocktailIngredients = [] };

        await using (var context = new CocktailDbContext(options))
        {
            await context.AddRangeAsync(cocktail);
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new CocktailDbContext(options))
        {
            var service = new CocktailService(context);
            await service.SetFavoriteAsync(1, false);
        }

        // Assert
        await using (var context = new CocktailDbContext(options))
        {
            var updatedCocktail = await context.Cocktails.SingleAsync(c => c.Id == 1);
            Assert.False(updatedCocktail.IsFavorite);
        }
    }

    [Fact]
    public async Task SetFavoriteAsync_WithInvalidCocktailId_ThrowsException()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(SetFavoriteAsync_WithInvalidCocktailId_ThrowsException));

        // Act & Assert
        await using var context = new CocktailDbContext(options);
        var service = new CocktailService(context);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetFavoriteAsync(999, true));
    }
}
