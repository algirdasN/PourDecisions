using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PourDecisions.Application.Services;
using PourDecisions.Core.Data;
using PourDecisions.Core.Entities;
using PourDecisions.Core.Enums;

namespace PourDecisions.UnitTests.Services;

public class BottleServiceTests
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
    public async Task AddBottleAsync_WithNewIngredientType_CreatesTypeAndBottle()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        var typeName = "gin";
        var bottleName = "Bombay Sapphire";
        var volume = 700;
        var fillLevel = FillLevel.Full;

        // Act
        await using (var context = CreateContext(connection))
        {
            var service = new BottleService(context);
            await service.AddBottleAsync(typeName, bottleName, volume, fillLevel);
        }

        // Assert
        await using (var context = CreateContext(connection))
        {
            var bottle = await context.Bottles.Include(b => b.Type).SingleAsync();
            Assert.Equal(bottleName, bottle.Name);
            Assert.Equal(volume, bottle.Volume);
            Assert.Equal(fillLevel, bottle.FillLevel);
            Assert.Equal("Gin", bottle.Type.Name); // Capitalized
            Assert.True(bottle.Type.IsTracked);
        }
    }

    [Fact]
    public async Task AddBottleAsync_WithExistingIngredientType_UsesExistingTypeAndSetsTracked()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        var ingredientType = new IngredientType { Id = 1, Name = "Gin", IsTracked = false };

        await using (var context = CreateContext(connection))
        {
            await context.IngredientTypes.AddAsync(ingredientType);
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = CreateContext(connection))
        {
            var service = new BottleService(context);
            await service.AddBottleAsync("GIN", "Beefeater", 1000, FillLevel.Half);
        }

        // Assert
        await using (var context = CreateContext(connection))
        {
            var bottle = await context.Bottles.Include(b => b.Type).SingleAsync();
            Assert.Equal(1, bottle.Type.Id);
            Assert.True(bottle.Type.IsTracked);
        }
    }

    [Fact]
    public async Task GetBottlesWithTypeAsync_ReturnsAllBottlesWithTypeLoaded()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        var type = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var bottle1 = new Bottle { Id = 1, Name = "Bottle 1", TypeId = 1, Type = type };
        var bottle2 = new Bottle { Id = 2, Name = "Bottle 2", TypeId = 1, Type = type };

        await using (var context = CreateContext(connection))
        {
            await context.IngredientTypes.AddAsync(type);
            await context.Bottles.AddRangeAsync(bottle1, bottle2);
            await context.SaveChangesAsync();
        }

        // Act
        List<Bottle> result;
        await using (var context = CreateContext(connection))
        {
            var service = new BottleService(context);
            result = await service.GetBottlesWithTypeAsync();
        }

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.NotNull(b.Type));
        Assert.Contains(result, b => b.Name == "Bottle 1");
        Assert.Contains(result, b => b.Name == "Bottle 2");
    }

    [Fact]
    public async Task GetBottlesOfTypeAsync_ReturnsOnlyBottlesOfSpecifiedType()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        var type1 = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var type2 = new IngredientType { Id = 2, Name = "Vodka", IsTracked = true };
        var bottle1 = new Bottle { Id = 1, Name = "Gin Bottle", TypeId = 1, Type = type1 };
        var bottle2 = new Bottle { Id = 2, Name = "Vodka Bottle", TypeId = 2, Type = type2 };

        await using (var context = CreateContext(connection))
        {
            await context.IngredientTypes.AddRangeAsync(type1, type2);
            await context.Bottles.AddRangeAsync(bottle1, bottle2);
            await context.SaveChangesAsync();
        }

        // Act
        List<Bottle> result;
        await using (var context = CreateContext(connection))
        {
            var service = new BottleService(context);
            result = await service.GetBottlesOfTypeAsync(1);
        }

        // Assert
        Assert.Single(result);
        Assert.Equal("Gin Bottle", result[0].Name);
    }

    [Fact]
    public async Task DeleteBottleAsync_RemovesBottleFromDatabase()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        var type = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var bottle = new Bottle { Id = 1, Name = "To Delete", TypeId = 1, Type = type };

        await using (var context = CreateContext(connection))
        {
            await context.IngredientTypes.AddAsync(type);
            await context.Bottles.AddAsync(bottle);
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = CreateContext(connection))
        {
            var service = new BottleService(context);
            await service.DeleteBottleAsync(1);
        }

        // Assert
        await using (var context = CreateContext(connection))
        {
            Assert.Empty(await context.Bottles.ToListAsync());
        }
    }

    [Fact]
    public async Task UpdateBottleFillLevelAsync_UpdatesFillLevel()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        var type = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var bottle = new Bottle
            { Id = 1, Name = "Test Bottle", FillLevel = FillLevel.Full, TypeId = 1, Type = type };

        await using (var context = CreateContext(connection))
        {
            await context.IngredientTypes.AddAsync(type);
            await context.Bottles.AddAsync(bottle);
            await context.SaveChangesAsync();
        }

        // Act
        await using (var context = CreateContext(connection))
        {
            var service = new BottleService(context);
            await service.UpdateBottleFillLevelAsync(1, FillLevel.Quarter);
        }

        // Assert
        await using (var context = CreateContext(connection))
        {
            var updatedBottle = await context.Bottles.SingleAsync();
            Assert.Equal(FillLevel.Quarter, updatedBottle.FillLevel);
        }
    }

    [Fact]
    public async Task UpdateBottleFillLevelAsync_WithInvalidId_ThrowsException()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        // Act & Assert
        await using var context = CreateContext(connection);
        var service = new BottleService(context);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateBottleFillLevelAsync(999, FillLevel.Quarter));
    }
}
