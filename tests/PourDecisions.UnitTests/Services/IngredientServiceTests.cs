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

    private static async Task SeedMergeAsync(SqliteConnection connection, bool conflict = false,
        bool sourceTracked = true, bool targetTracked = false, bool sourceBottle = true)
    {
        await using var context = CreateContext(connection);
        var source = new IngredientType { Id = 1, Name = "Gin", IsTracked = sourceTracked };
        var target = new IngredientType { Id = 2, Name = "vOdKa", IsTracked = targetTracked };
        var other = new IngredientType { Id = 3, Name = "Rum", IsTracked = true };
        context.IngredientTypes.AddRange(source, target, other);
        context.Cocktails.AddRange(
            new Cocktail
            {
                Id = 1, Name = "Source cocktail", Instructions = "Stir",
                CocktailIngredients = new List<CocktailIngredient>
                {
                    new() { Id = 1, TypeId = 1, AmountValue = 60, AmountUnit = AmountUnit.Ml, SortOrder = 2 },
                    new() { Id = 2, TypeId = conflict ? 2 : 3, AmountValue = 20, AmountUnit = AmountUnit.Ml }
                }
            },
            new Cocktail
            {
                Id = 2, Name = "Target cocktail", Instructions = "Shake",
                CocktailIngredients = new List<CocktailIngredient>
                {
                    new() { Id = 3, TypeId = 2, AmountValue = 30, AmountUnit = AmountUnit.Ml }
                }
            });
        if (sourceBottle)
        {
            context.Bottles.Add(new Bottle
                { Id = 1, Name = "Source bottle", Volume = 700, Type = source, FillLevel = FillLevel.Full });
        }
        context.Bottles.Add(new Bottle
            { Id = 2, Name = "Other bottle", Volume = 500, Type = other, FillLevel = FillLevel.Full });
        await context.SaveChangesAsync();
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
        RenameIngredientResult result;
        await using (var context = CreateContext(connection))
        {
            var service = new IngredientService(context);
            result = await service.RenameIngredientAsync(1, shortName);
        }

        // Assert
        var error = Assert.IsType<RenameIngredientResult.Error>(result);
        Assert.Equal("Ingredient type name must be at least 3 characters long.", error.ErrorMessage);
    }


    [Fact]
    public async Task RenameIngredientAsync_TypeNotFound_ReturnsError()
    {
        // Arrange
        await using var connection = CreateOpenConnection();

        // Act
        RenameIngredientResult result;
        await using (var context = CreateContext(connection))
        {
            var service = new IngredientService(context);
            result = await service.RenameIngredientAsync(999, "Tequila");
        }

        // Assert
        var error = Assert.IsType<RenameIngredientResult.Error>(result);
        Assert.Equal("Ingredient type with ID 999 not found.", error.ErrorMessage);
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
        RenameIngredientResult result;
        await using (var context = CreateContext(connection))
        {
            var service = new IngredientService(context);
            result = await service.RenameIngredientAsync(1, "gin");
        }

        // Assert
        var error = Assert.IsType<RenameIngredientResult.Error>(result);
        Assert.Equal("Enter a new ingredient type name.", error.ErrorMessage);
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
        RenameIngredientResult result;
        await using (var context = CreateContext(connection))
        {
            var service = new IngredientService(context);
            result = await service.RenameIngredientAsync(1, "Vodka");
        }

        // Assert
        var collision = Assert.IsType<RenameIngredientResult.NameCollisionCanMerge>(result);
        Assert.Equal("Vodka", collision.NewName);
        Assert.Equal(2, collision.ConflictId);
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
        RenameIngredientResult result;
        await using (var context = CreateContext(connection))
        {
            var service = new IngredientService(context);
            result = await service.RenameIngredientAsync(1, "london dry gin");
        }

        // Assert
        await using (var context = CreateContext(connection))
        {
            var success = Assert.IsType<RenameIngredientResult.Success>(result);
            Assert.Equal("London Dry Gin", success.NewName);

            var updated = await context.IngredientTypes.FindAsync(1);
            Assert.NotNull(updated);
            Assert.Equal("London Dry Gin", updated.Name);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RenameIngredientAsync_CollisionReportsMergePossibilityWithoutPersistingChanges(bool conflict)
    {
        await using var connection = CreateOpenConnection();
        await SeedMergeAsync(connection, conflict);

        await using (var context = CreateContext(connection))
        {
            var result = await new IngredientService(context).RenameIngredientAsync(1, "  VODKA  ");
            if (conflict)
            {
                var blocked = Assert.IsType<RenameIngredientResult.NameCollisionMergeBlocked>(result);
                Assert.Equal(new[] { "Source cocktail" }, blocked.CocktailNames);
            }
            else
            {
                var collision = Assert.IsType<RenameIngredientResult.NameCollisionCanMerge>(result);
                Assert.Equal("Vodka", collision.NewName);
                Assert.Equal(2, collision.ConflictId);
            }
        }

        await using (var context = CreateContext(connection))
        {
            Assert.Equal("Gin", (await context.IngredientTypes.FindAsync(1))!.Name);
            Assert.Equal("vOdKa", (await context.IngredientTypes.FindAsync(2))!.Name);
            Assert.Equal(1, (await context.Bottles.FindAsync(1))!.TypeId);
            Assert.Equal(1, (await context.CocktailIngredients.FindAsync(1))!.TypeId);
        }
    }

    [Theory]
    [InlineData(1, 1, "Cannot merge an ingredient type into itself.")]
    [InlineData(1, 999, "One or both ingredient types not found.")]
    [InlineData(999, 2, "One or both ingredient types not found.")]
    [InlineData(998, 999, "One or both ingredient types not found.")]
    public async Task MergeIngredientTypesAsync_InvalidIds_ReturnsErrorWithoutChanges(int sourceId, int targetId,
        string message)
    {
        await using var connection = CreateOpenConnection();
        await SeedMergeAsync(connection);

        await using (var context = CreateContext(connection))
        {
            var result = await new IngredientService(context).MergeIngredientTypesAsync(sourceId, targetId);
            Assert.Equal(message, Assert.IsType<MergeIngredientResult.Error>(result).ErrorMessage);
        }

        await using (var context = CreateContext(connection))
        {
            Assert.Equal(3, await context.IngredientTypes.CountAsync());
            Assert.Equal(1, (await context.Bottles.FindAsync(1))!.TypeId);
            Assert.Equal(1, (await context.CocktailIngredients.FindAsync(1))!.TypeId);
            Assert.False((await context.IngredientTypes.FindAsync(2))!.IsTracked);
        }
    }

    [Fact]
    public async Task MergeIngredientTypesAsync_RecipeConflict_DoesNotChangePersistedData()
    {
        await using var connection = CreateOpenConnection();
        await SeedMergeAsync(connection, conflict: true);

        await using (var context = CreateContext(connection))
        {
            var result = await new IngredientService(context).MergeIngredientTypesAsync(1, 2);
            Assert.Equal(new[] { "Source cocktail" }, Assert.IsType<MergeIngredientResult.RecipeConflict>(result).CocktailNames);
        }

        await using (var context = CreateContext(connection))
        {
            Assert.Equal(3, await context.IngredientTypes.CountAsync());
            Assert.False((await context.IngredientTypes.FindAsync(2))!.IsTracked);
            Assert.Equal(1, (await context.Bottles.FindAsync(1))!.TypeId);
            Assert.Equal(1, (await context.CocktailIngredients.FindAsync(1))!.TypeId);
            Assert.Equal(2, (await context.CocktailIngredients.FindAsync(2))!.TypeId);
        }
    }

    [Fact]
    public async Task MergeIngredientTypesAsync_SaveFailure_RollsBackAllChanges()
    {
        await using var connection = CreateOpenConnection();
        await SeedMergeAsync(connection);
        await using (var context = CreateContext(connection))
        {
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER RejectIngredientDelete BEFORE DELETE ON IngredientTypes BEGIN SELECT RAISE(ABORT, 'delete rejected'); END;");
        }

        await using (var context = CreateContext(connection))
        {
            var service = new IngredientService(context);
            await Assert.ThrowsAsync<DbUpdateException>(() => service.MergeIngredientTypesAsync(1, 2));
        }

        await using (var context = CreateContext(connection))
        {
            Assert.NotNull(await context.IngredientTypes.FindAsync(1));
            Assert.False((await context.IngredientTypes.FindAsync(2))!.IsTracked);
            Assert.Equal(1, (await context.Bottles.FindAsync(1))!.TypeId);
            Assert.Equal(1, (await context.CocktailIngredients.FindAsync(1))!.TypeId);
        }
    }

    [Theory]
    [InlineData(true, false, true, true)]
    [InlineData(true, false, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, true, false, true)]
    public async Task MergeIngredientTypesAsync_ValidMerge_PersistsReferencesAndTrackedStatus(
        bool sourceTracked, bool targetTracked, bool sourceBottle, bool expectedTracked)
    {
        await using var connection = CreateOpenConnection();
        await SeedMergeAsync(connection, sourceTracked: sourceTracked, targetTracked: targetTracked,
            sourceBottle: sourceBottle);

        await using (var context = CreateContext(connection))
        {
            var result = await new IngredientService(context).MergeIngredientTypesAsync(1, 2);
            Assert.Equal(expectedTracked, Assert.IsType<MergeIngredientResult.Success>(result).IsTargetTracked);
        }

        await using (var context = CreateContext(connection))
        {
            Assert.Null(await context.IngredientTypes.FindAsync(1));
            var target = await context.IngredientTypes.FindAsync(2);
            Assert.NotNull(target);
            Assert.Equal("vOdKa", target.Name);
            Assert.Equal(expectedTracked, target.IsTracked);
            Assert.NotNull(await context.IngredientTypes.FindAsync(3));
            var ingredient = await context.CocktailIngredients.FindAsync(1);
            Assert.NotNull(ingredient);
            Assert.Equal(2, ingredient.TypeId);
            Assert.Equal(60, ingredient.AmountValue);
            Assert.Equal(AmountUnit.Ml, ingredient.AmountUnit);
            Assert.Equal(2, ingredient.SortOrder);
            Assert.Equal(3, (await context.CocktailIngredients.FindAsync(2))!.TypeId);
            Assert.Equal(2, (await context.CocktailIngredients.FindAsync(3))!.TypeId);
            if (sourceBottle)
            {
                var bottle = await context.Bottles.FindAsync(1);
                Assert.NotNull(bottle);
                Assert.Equal(2, bottle.TypeId);
                Assert.Equal("Source bottle", bottle.Name);
                Assert.Equal(700, bottle.Volume);
            }
            Assert.Equal(3, (await context.Bottles.FindAsync(2))!.TypeId);
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
