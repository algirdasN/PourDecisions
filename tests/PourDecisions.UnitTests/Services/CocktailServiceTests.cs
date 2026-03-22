using Microsoft.EntityFrameworkCore;
using PourDecisions.Application.Models;
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
    public async Task GetWithIngredientsAsync_ReturnsCocktailWithIngredients()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetWithIngredientsAsync_ReturnsCocktailWithIngredients));
        var ingredientType = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var cocktail = new Cocktail
        {
            Id = 1,
            Name = "Martini",
            Instructions = "Stir",
            CocktailIngredients = new List<CocktailIngredient>
            {
                new() { Id = 1, Type = ingredientType, AmountValue = 60, AmountUnit = AmountUnit.Ml, SortOrder = 0 }
            }
        };

        await using (var context = new CocktailDbContext(options))
        {
            await context.AddRangeAsync(ingredientType, cocktail);
            await context.SaveChangesAsync();
        }

        // Act
        Cocktail result;
        await using (var context = new CocktailDbContext(options))
        {
            var service = new CocktailService(context);
            result = await service.GetWithIngredientsAsync(1);
        }

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Martini", result.Name);
        Assert.Single(result.CocktailIngredients);
        Assert.Equal("Gin", result.CocktailIngredients.First().Type.Name);
    }

    [Fact]
    public async Task GetWithIngredientsAsync_WithInvalidId_ThrowsException()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetWithIngredientsAsync_WithInvalidId_ThrowsException));

        // Act & Assert
        await using var context = new CocktailDbContext(options);
        var service = new CocktailService(context);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetWithIngredientsAsync(999));
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
            AmountUnit = AmountUnit.Ml
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
    public async Task GetAllSummariesAsync_ReturnsAllSummaries()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetAllSummariesAsync_ReturnsAllSummaries));
        var cocktail1 = new Cocktail
            { Id = 1, Name = "Martini", IsFavorite = true, Instructions = "Stir", CocktailIngredients = [] };
        var cocktail2 = new Cocktail
            { Id = 2, Name = "Negroni", IsFavorite = false, Instructions = "Stir", CocktailIngredients = [] };

        await using (var context = new CocktailDbContext(options))
        {
            await context.AddRangeAsync(cocktail1, cocktail2);
            await context.SaveChangesAsync();
        }

        // Act
        List<CocktailEditSummary> result;
        await using (var context = new CocktailDbContext(options))
        {
            var service = new CocktailService(context);
            result = await service.GetAllSummariesAsync();
        }

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s is { Id: 1, Name: "Martini", IsFavorite: true });
        Assert.Contains(result, s => s is { Id: 2, Name: "Negroni", IsFavorite: false });
    }

    [Fact]
    public async Task AddCocktailAsync_AddsNewCocktail()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(AddCocktailAsync_AddsNewCocktail));
        var summaries = new List<CocktailIngredientSummary>
        {
            new(60, AmountUnit.Ml, "Gin"),
            new(15, AmountUnit.Ml, "Vermouth")
        };

        // Act
        int cocktailId;
        await using (var context = new CocktailDbContext(options))
        {
            var service = new CocktailService(context);
            cocktailId = await service.AddCocktailAsync("martini", summaries, "Stir with ice");
        }

        // Assert
        await using (var context = new CocktailDbContext(options))
        {
            var cocktail = await context.Cocktails
                .Include(c => c.CocktailIngredients)
                .ThenInclude(ci => ci.Type)
                .SingleAsync(c => c.Id == cocktailId);

            Assert.Equal("Martini", cocktail.Name); // TitleCase
            Assert.Equal("Stir with ice", cocktail.Instructions);
            Assert.Equal(2, cocktail.CocktailIngredients.Count);
            Assert.Contains(cocktail.CocktailIngredients, ci => ci.Type.Name == "Gin");
            Assert.Contains(cocktail.CocktailIngredients, ci => ci.Type.Name == "Vermouth");
        }
    }

    [Fact]
    public async Task EditCocktailAsync_UpdatesExistingCocktail()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(EditCocktailAsync_UpdatesExistingCocktail));
        var ingredientType = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var cocktail = new Cocktail
        {
            Id = 1,
            Name = "Old Martini",
            Instructions = "Old instructions",
            CocktailIngredients = new List<CocktailIngredient>
            {
                new() { Id = 1, Type = ingredientType, AmountValue = 60, AmountUnit = AmountUnit.Ml, SortOrder = 0 }
            }
        };

        await using (var context = new CocktailDbContext(options))
        {
            await context.AddRangeAsync(ingredientType, cocktail);
            await context.SaveChangesAsync();
        }

        var newSummaries = new List<CocktailIngredientSummary>
        {
            new(60, AmountUnit.Ml, "Vodka")
        };

        // Act
        await using (var context = new CocktailDbContext(options))
        {
            var service = new CocktailService(context);
            await service.EditCocktailAsync(1, "New Martini", newSummaries, "New instructions");
        }

        // Assert
        await using (var context = new CocktailDbContext(options))
        {
            var updatedCocktail = await context.Cocktails
                .Include(c => c.CocktailIngredients)
                .ThenInclude(ci => ci.Type)
                .SingleAsync(c => c.Id == 1);

            Assert.Equal("New Martini", updatedCocktail.Name);
            Assert.Equal("New instructions", updatedCocktail.Instructions);
            Assert.Single(updatedCocktail.CocktailIngredients);
            Assert.Equal("Vodka", updatedCocktail.CocktailIngredients.First().Type.Name);
        }
    }

    [Fact]
    public async Task DeleteCocktailAsync_DeletesCocktail()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(DeleteCocktailAsync_DeletesCocktail));
        var cocktail = new Cocktail { Id = 1, Name = "Martini", Instructions = "Stir", CocktailIngredients = [] };

        await using (var context = new CocktailDbContext(options))
        {
            await context.AddAsync(cocktail);
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = new CocktailDbContext(options))
        {
            var service = new CocktailService(context);
            await service.DeleteCocktailAsync(1);
        }

        // Assert
        await using (var context = new CocktailDbContext(options))
        {
            var deletedCocktail = await context.Cocktails.FirstOrDefaultAsync(c => c.Id == 1);
            Assert.Null(deletedCocktail);
        }
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
