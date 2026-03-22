using Microsoft.EntityFrameworkCore;
using PourDecisions.Application.Services;
using PourDecisions.Core.Data;
using PourDecisions.Core.Entities;

namespace PourDecisions.UnitTests.Services;

public class IngredientServiceTests
{
    private static DbContextOptions<CocktailDbContext> CreateInMemoryOptions(string databaseName)
    {
        return new DbContextOptionsBuilder<CocktailDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
    }

    [Fact]
    public async Task GetIngredientTypeNamesAsync_ReturnsAllNamesInAlphabeticalOrder()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetIngredientTypeNamesAsync_ReturnsAllNamesInAlphabeticalOrder));
        var types = new List<IngredientType>
        {
            new() { Id = 1, Name = "Gin", IsTracked = true },
            new() { Id = 2, Name = "Vodka", IsTracked = true },
            new() { Id = 3, Name = "Bourbon", IsTracked = false }
        };

        await using (var context = new CocktailDbContext(options))
        {
            await context.IngredientTypes.AddRangeAsync(types);
            await context.SaveChangesAsync();
        }

        // Act
        List<string> result;
        await using (var context = new CocktailDbContext(options))
        {
            var service = new IngredientService(context);
            result = await service.GetIngredientTypeNamesAsync();
        }

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Bourbon", result[0]);
        Assert.Equal("Gin", result[1]);
        Assert.Equal("Vodka", result[2]);
    }

    [Fact]
    public async Task GetTrackedIngredientTypeNamesAsync_ReturnsOnlyTrackedNamesInAlphabeticalOrder()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetTrackedIngredientTypeNamesAsync_ReturnsOnlyTrackedNamesInAlphabeticalOrder));
        var types = new List<IngredientType>
        {
            new() { Id = 1, Name = "Gin", IsTracked = true },
            new() { Id = 2, Name = "Vodka", IsTracked = true },
            new() { Id = 3, Name = "Bourbon", IsTracked = true },
            new() { Id = 4, Name = "Untracked", IsTracked = false }
        };

        await using (var context = new CocktailDbContext(options))
        {
            await context.IngredientTypes.AddRangeAsync(types);
            await context.SaveChangesAsync();
        }

        // Act
        List<string> result;
        await using (var context = new CocktailDbContext(options))
        {
            var service = new IngredientService(context);
            result = await service.GetTrackedIngredientTypeNamesAsync();
        }

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Bourbon", result[0]);
        Assert.Equal("Gin", result[1]);
        Assert.Equal("Vodka", result[2]);
        Assert.DoesNotContain("Untracked", result);
    }

    [Fact]
    public async Task GetTrackedIngredientTypeNamesAsync_WithNoTrackedIngredients_ReturnsEmptyList()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetTrackedIngredientTypeNamesAsync_WithNoTrackedIngredients_ReturnsEmptyList));
        var type = new IngredientType { Id = 1, Name = "Untracked", IsTracked = false };

        await using (var context = new CocktailDbContext(options))
        {
            await context.IngredientTypes.AddAsync(type);
            await context.SaveChangesAsync();
        }

        // Act
        List<string> result;
        await using (var context = new CocktailDbContext(options))
        {
            var service = new IngredientService(context);
            result = await service.GetTrackedIngredientTypeNamesAsync();
        }

        // Assert
        Assert.Empty(result);
    }
}
