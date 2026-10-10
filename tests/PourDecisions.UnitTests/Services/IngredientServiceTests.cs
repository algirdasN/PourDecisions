using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PourDecisions.Application.Models;
using PourDecisions.Application.Services;
using PourDecisions.Core.Data;
using PourDecisions.Core.Entities;
using PourDecisions.Core.Enums;

namespace PourDecisions.UnitTests.Services;

public class IngredientServiceTests
{
    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        using var context = CreateContext(connection);
        context.Database.EnsureCreated();
        return connection;
    }

    private static CocktailDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<CocktailDbContext>()
            .UseSqlite(connection)
            .Options;
        return new CocktailDbContext(options);
    }

    [Fact]
    public async Task GetIngredientTypesAsync_ReturnsAllTypesInAlphabeticalOrder()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        var types = new List<IngredientType>
        {
            new() { Id = 1, Name = "Vodka", IsTracked = true },
            new() { Id = 2, Name = "Gin", IsTracked = true },
            new() { Id = 3, Name = "Bourbon", IsTracked = false }
        };

        await using (var context = CreateContext(connection))
        {
            await context.IngredientTypes.AddRangeAsync(types);
            await context.SaveChangesAsync();
        }

        // Act
        List<IngredientType> result;
        await using (var context = CreateContext(connection))
        {
            var service = new IngredientService(context);
            result = await service.GetIngredientTypesAsync();
        }

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Bourbon", result[0].Name);
        Assert.Equal("Gin", result[1].Name);
        Assert.Equal("Vodka", result[2].Name);
    }

    [Fact]
    public async Task GetIngredientTypeNamesAsync_ReturnsAllNamesInAlphabeticalOrder()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        var types = new List<IngredientType>
        {
            new() { Id = 1, Name = "Gin", IsTracked = true },
            new() { Id = 2, Name = "Vodka", IsTracked = true },
            new() { Id = 3, Name = "Bourbon", IsTracked = false }
        };

        await using (var context = CreateContext(connection))
        {
            await context.IngredientTypes.AddRangeAsync(types);
            await context.SaveChangesAsync();
        }

        // Act
        List<string> result;
        await using (var context = CreateContext(connection))
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
        await using var connection = CreateOpenConnection();

        var types = new List<IngredientType>
        {
            new() { Id = 1, Name = "Gin", IsTracked = true },
            new() { Id = 2, Name = "Vodka", IsTracked = true },
            new() { Id = 3, Name = "Bourbon", IsTracked = true },
            new() { Id = 4, Name = "Untracked", IsTracked = false }
        };

        await using (var context = CreateContext(connection))
        {
            await context.IngredientTypes.AddRangeAsync(types);
            await context.SaveChangesAsync();
        }

        // Act
        List<string> result;
        await using (var context = CreateContext(connection))
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
        await using var connection = CreateOpenConnection();

        var type = new IngredientType { Id = 1, Name = "Untracked", IsTracked = false };

        await using (var context = CreateContext(connection))
        {
            await context.IngredientTypes.AddAsync(type);
            await context.SaveChangesAsync();
        }

        // Act
        List<string> result;
        await using (var context = CreateContext(connection))
        {
            var service = new IngredientService(context);
            result = await service.GetTrackedIngredientTypeNamesAsync();
        }

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    [InlineData("  a  ")]
    public async Task RenameIngredientAsync_NameTooShort_ReturnsError(string shortName)
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        var type = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };

        await using (var context = CreateContext(connection))
        {
            await context.IngredientTypes.AddAsync(type);
            await context.SaveChangesAsync();
        }

        // Act
        Result result;
        await using (var context = CreateContext(connection))
        {
            var service = new IngredientService(context);
            result = await service.RenameIngredientAsync(1, shortName);
        }

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Ingredient type name must be at least 3 characters long.", result.ErrorMessage);
    }


    [Fact]
    public async Task RenameIngredientAsync_TypeNotFound_ReturnsError()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        // Act
        Result result;
        await using (var context = CreateContext(connection))
        {
            var service = new IngredientService(context);
            result = await service.RenameIngredientAsync(999, "Tequila");
        }

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Ingredient type with ID 999 not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task RenameIngredientAsync_SameName_ReturnsError()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        var type = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };

        await using (var context = CreateContext(connection))
        {
            await context.IngredientTypes.AddAsync(type);
            await context.SaveChangesAsync();
        }

        // Act
        Result result;
        await using (var context = CreateContext(connection))
        {
            var service = new IngredientService(context);
            result = await service.RenameIngredientAsync(1, "gin");
        }

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Enter a new ingredient type name.", result.ErrorMessage);
    }

    [Fact]
    public async Task RenameIngredientAsync_NameAlreadyExists_ReturnsError()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        var type1 = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var type2 = new IngredientType { Id = 2, Name = "Vodka", IsTracked = true };

        await using (var context = CreateContext(connection))
        {
            await context.IngredientTypes.AddRangeAsync(type1, type2);
            await context.SaveChangesAsync();
        }

        // Act
        Result result;
        await using (var context = CreateContext(connection))
        {
            var service = new IngredientService(context);
            result = await service.RenameIngredientAsync(1, "Vodka");
        }

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Ingredient type 'Vodka' already exists.", result.ErrorMessage);
    }

    [Fact]
    public async Task RenameIngredientAsync_ValidNewName_RenamesAndReturnsSuccess()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        var type = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };

        await using (var context = CreateContext(connection))
        {
            await context.IngredientTypes.AddAsync(type);
            await context.SaveChangesAsync();
        }

        // Act
        Result<string> result;
        await using (var context = CreateContext(connection))
        {
            var service = new IngredientService(context);
            result = await service.RenameIngredientAsync(1, "london dry gin");
        }

        // Assert
        await using (var context = CreateContext(connection))
        {
            Assert.True(result.IsSuccess);
            Assert.Equal("London Dry Gin", result.Value);

            var updated = await context.IngredientTypes.FindAsync(1);
            Assert.NotNull(updated);
            Assert.Equal("London Dry Gin", updated.Name);
        }
    }

    [Fact]
    public async Task PreviewIngredientTypeModifyAsync_ReturnsCocktailsAndBottles()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        var type = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var cocktail = new Cocktail
        {
            Id = 1,
            Name = "Martini",
            Instructions = "Stir",
            CocktailIngredients = new List<CocktailIngredient>
            {
                new() { Id = 1, TypeId = 1, AmountValue = 60, AmountUnit = AmountUnit.Ml }
            }
        };
        var bottle = new Bottle
            { Id = 1, Name = "Bombay", Volume = 700, TypeId = 1, Type = type, FillLevel = FillLevel.Full };

        await using (var context = CreateContext(connection))
        {
            await context.IngredientTypes.AddAsync(type);
            await context.Cocktails.AddAsync(cocktail);
            await context.Bottles.AddAsync(bottle);
            await context.SaveChangesAsync();
        }

        // Act
        IngredientTypeModifyImpact impact;
        await using (var context = CreateContext(connection))
        {
            var service = new IngredientService(context);
            impact = await service.PreviewIngredientTypeModifyAsync(1);
        }

        // Assert
        Assert.Single(impact.CocktailNames);
        Assert.Equal("Martini", impact.CocktailNames[0]);
        Assert.Single(impact.BottleInfoList);
        Assert.Equal("Bombay", impact.BottleInfoList[0].Name);
        Assert.Equal(700, impact.BottleInfoList[0].Volume);
    }

    [Fact]
    public async Task UpdateIngredientTypeTrackedAsync_SetToTrue_UpdatesTrackedStatus()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        var type = new IngredientType { Id = 1, Name = "Gin", IsTracked = false };

        await using (var context = CreateContext(connection))
        {
            await context.IngredientTypes.AddAsync(type);
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = CreateContext(connection))
        {
            var service = new IngredientService(context);
            await service.UpdateIngredientTypeTrackedAsync(1, true);
        }

        // Assert
        await using (var context = CreateContext(connection))
        {
            var updated = await context.IngredientTypes.FindAsync(1);
            Assert.NotNull(updated);
            Assert.True(updated.IsTracked);
        }
    }

    [Fact]
    public async Task UpdateIngredientTypeTrackedAsync_SetToFalse_WithBottlesNotAllowed_ThrowsException()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        var type = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var bottle = new Bottle
            { Id = 1, Name = "Bombay", Volume = 700, TypeId = 1, Type = type, FillLevel = FillLevel.Full };

        await using (var context = CreateContext(connection))
        {
            await context.IngredientTypes.AddAsync(type);
            await context.Bottles.AddAsync(bottle);
            await context.SaveChangesAsync();
        }

        // Act & Assert
        await using (var context = CreateContext(connection))
        {
            var service = new IngredientService(context);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdateIngredientTypeTrackedAsync(1, false, false));
        }
    }

    [Fact]
    public async Task UpdateIngredientTypeTrackedAsync_SetToFalse_WithBottlesAllowed_DeletesBottlesAndUpdatesStatus()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        var type = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var bottle1 = new Bottle
            { Id = 1, Name = "Bombay", Volume = 700, TypeId = 1, Type = type, FillLevel = FillLevel.Full };
        var bottle2 = new Bottle
            { Id = 2, Name = "Tanqueray", Volume = 1000, TypeId = 1, Type = type, FillLevel = FillLevel.Full };

        await using (var context = CreateContext(connection))
        {
            await context.IngredientTypes.AddAsync(type);
            await context.Bottles.AddRangeAsync(bottle1, bottle2);
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = CreateContext(connection))
        {
            var service = new IngredientService(context);
            await service.UpdateIngredientTypeTrackedAsync(1, false, true);
        }

        // Assert
        await using (var context = CreateContext(connection))
        {
            var updated = await context.IngredientTypes.FindAsync(1);
            Assert.NotNull(updated);
            Assert.False(updated.IsTracked);
            Assert.Empty(await context.Bottles.Where(b => b.TypeId == 1).ToListAsync());
        }
    }

    [Fact]
    public async Task DeleteIngredientTypeAsync_UsedInCocktails_ThrowsException()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        var type = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var cocktail = new Cocktail
        {
            Id = 1,
            Name = "Martini",
            Instructions = "Stir",
            CocktailIngredients = new List<CocktailIngredient>
            {
                new() { Id = 1, TypeId = 1, AmountValue = 60, AmountUnit = AmountUnit.Ml }
            }
        };

        await using (var context = CreateContext(connection))
        {
            await context.IngredientTypes.AddAsync(type);
            await context.Cocktails.AddAsync(cocktail);
            await context.SaveChangesAsync();
        }

        // Act & Assert
        await using (var context = CreateContext(connection))
        {
            var service = new IngredientService(context);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteIngredientTypeAsync(1, true));
        }
    }

    [Fact]
    public async Task DeleteIngredientTypeAsync_UsedInBottlesNotAllowed_ThrowsException()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        var type = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var bottle = new Bottle
            { Id = 1, Name = "Bombay", Volume = 700, TypeId = 1, Type = type, FillLevel = FillLevel.Full };

        await using (var context = CreateContext(connection))
        {
            await context.IngredientTypes.AddAsync(type);
            await context.Bottles.AddAsync(bottle);
            await context.SaveChangesAsync();
        }

        // Act & Assert
        await using (var context = CreateContext(connection))
        {
            var service = new IngredientService(context);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteIngredientTypeAsync(1, false));
        }
    }

    [Fact]
    public async Task DeleteIngredientTypeAsync_NoUsages_DeletesSuccessfully()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        var type = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        await using (var context = CreateContext(connection))
        {
            await context.IngredientTypes.AddAsync(type);
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = CreateContext(connection))
        {
            var service = new IngredientService(context);
            await service.DeleteIngredientTypeAsync(1);
        }

        // Assert
        await using (var context = CreateContext(connection))
        {
            Assert.Empty(await context.IngredientTypes.ToListAsync());
        }
    }
}
