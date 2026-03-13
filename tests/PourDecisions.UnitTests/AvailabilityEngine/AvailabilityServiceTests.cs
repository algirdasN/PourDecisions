using Microsoft.EntityFrameworkCore;
using PourDecisions.Application.AvailabilityEngine;
using PourDecisions.Core.Data;
using PourDecisions.Core.Entities;
using PourDecisions.Core.Enums;

namespace PourDecisions.UnitTests.AvailabilityEngine;

public class AvailabilityServiceTests
{
    private static DbContextOptions<CocktailDbContext> CreateInMemoryOptions(string databaseName)
    {
        return new DbContextOptionsBuilder<CocktailDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
    }

    [Fact]
    public async Task GetCocktailAvailabilityAsync_AllIngredientsAvailable_ReturnsCocktailAsAvailable()
    {
        // Arrange
        var options = CreateInMemoryOptions(
            nameof(GetCocktailAvailabilityAsync_AllIngredientsAvailable_ReturnsCocktailAsAvailable));

        var ginType = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var vermouthType = new IngredientType { Id = 2, Name = "Vermouth", IsTracked = true };

        var ginBottle = new Bottle
            { Id = 1, FillLevel = FillLevel.Half, Name = "Tanqueray", TypeId = 1, Type = ginType };
        var vermouthBottle = new Bottle
            { Id = 2, FillLevel = FillLevel.Quarter, Name = "Noilly Prat", TypeId = 2, Type = vermouthType };

        ginType.Bottles = new List<Bottle> { ginBottle };
        vermouthType.Bottles = new List<Bottle> { vermouthBottle };

        var cocktail = new Cocktail
        {
            Id = 1,
            Name = "Martini",
            Instructions = "Stir",
            CocktailIngredients = new List<CocktailIngredient>
            {
                new()
                {
                    Id = 1, TypeId = 1, AmountValue = 60, AmountUnit = AmountUnit.Ml, IsOptional = false,
                    CocktailId = 1, Type = ginType
                },
                new()
                {
                    Id = 2, TypeId = 2, AmountValue = 30, AmountUnit = AmountUnit.Ml, IsOptional = false,
                    CocktailId = 1, Type = vermouthType
                }
            }
        };

        await using (var context = new CocktailDbContext(options))
        {
            await context.AddRangeAsync(ginType, vermouthType, ginBottle, vermouthBottle, cocktail);
            await context.SaveChangesAsync();
        }

        // Act
        Dictionary<int, AvailabilityResult> result;
        await using (var context = new CocktailDbContext(options))
        {
            var service = new AvailabilityService(context);
            result = await service.GetCocktailAvailabilityAsync();
        }

        // Assert
        Assert.True(result.ContainsKey(1));
        Assert.Equal(AvailabilityStatus.Available, result[1].Status);
    }

    [Fact]
    public async Task GetCocktailAvailabilityAsync_UntrackedIngredientMissing_TreatsAsAvailable()
    {
        // Arrange
        var options = CreateInMemoryOptions(
            nameof(GetCocktailAvailabilityAsync_UntrackedIngredientMissing_TreatsAsAvailable));

        var ginType = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var garnishType = new IngredientType { Id = 2, Name = "Lemon", IsTracked = false };

        var bottle = new Bottle { Id = 1, FillLevel = FillLevel.Half, Name = "Tanqueray", TypeId = 1, Type = ginType };
        ginType.Bottles = new List<Bottle> { bottle };

        var cocktail = new Cocktail
        {
            Id = 1,
            Name = "Martini",
            Instructions = "Stir",
            CocktailIngredients = new List<CocktailIngredient>
            {
                new()
                {
                    Id = 1, TypeId = 1, AmountValue = 60, AmountUnit = AmountUnit.Ml, IsOptional = false,
                    CocktailId = 1, Type = ginType
                },
                new()
                {
                    Id = 2, TypeId = 2, AmountValue = 1, AmountUnit = AmountUnit.Piece, IsOptional = false,
                    CocktailId = 1, Type = garnishType
                }
            }
        };

        await using (var context = new CocktailDbContext(options))
        {
            await context.AddRangeAsync(ginType, garnishType, bottle, cocktail);
            await context.SaveChangesAsync();
        }

        // Act
        Dictionary<int, AvailabilityResult> result;
        await using (var context = new CocktailDbContext(options))
        {
            var service = new AvailabilityService(context);
            result = await service.GetCocktailAvailabilityAsync();
        }

        // Assert - Untracked ingredient (Lemon) should be treated as available
        Assert.True(result.ContainsKey(1));
        Assert.Equal(AvailabilityStatus.Available, result[1].Status);
        Assert.Empty(result[1].MissingRequired);
    }

    [Fact]
    public async Task GetCocktailAvailabilityAsync_RequiredIngredientMissing_ReturnsCocktailAsUnavailable()
    {
        // Arrange
        var options = CreateInMemoryOptions(
            nameof(GetCocktailAvailabilityAsync_RequiredIngredientMissing_ReturnsCocktailAsUnavailable));

        var ginType = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var vermouthType = new IngredientType { Id = 2, Name = "Vermouth", IsTracked = true };

        var cocktail = new Cocktail
        {
            Id = 1,
            Name = "Martini",
            Instructions = "Stir",
            CocktailIngredients = new List<CocktailIngredient>
            {
                new()
                {
                    Id = 1, TypeId = 1, AmountValue = 60, AmountUnit = AmountUnit.Ml, IsOptional = false,
                    CocktailId = 1, Type = ginType
                },
                new()
                {
                    Id = 2, TypeId = 2, AmountValue = 30, AmountUnit = AmountUnit.Ml, IsOptional = false,
                    CocktailId = 1, Type = vermouthType
                }
            }
        };

        await using (var context = new CocktailDbContext(options))
        {
            await context.AddRangeAsync(ginType, vermouthType, cocktail);
            await context.SaveChangesAsync();
        }

        // Act
        Dictionary<int, AvailabilityResult> result;
        await using (var context = new CocktailDbContext(options))
        {
            var service = new AvailabilityService(context);
            result = await service.GetCocktailAvailabilityAsync();
        }

        // Assert - No bottles for either ingredient
        Assert.True(result.ContainsKey(1));
        Assert.Equal(AvailabilityStatus.Unavailable, result[1].Status);
        Assert.Equal(2, result[1].MissingRequired.Count);
    }

    [Fact]
    public async Task GetCocktailAvailabilityAsync_OptionalIngredientMissing_StillAvailable()
    {
        // Arrange
        var options = CreateInMemoryOptions(
            nameof(GetCocktailAvailabilityAsync_OptionalIngredientMissing_StillAvailable));

        var ginType = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var bitterType = new IngredientType { Id = 2, Name = "Bitters", IsTracked = true };

        var bottle = new Bottle { Id = 1, FillLevel = FillLevel.Half, Name = "Tanqueray", TypeId = 1, Type = ginType };
        ginType.Bottles = new List<Bottle> { bottle };

        var cocktail = new Cocktail
        {
            Id = 1,
            Name = "Martini",
            Instructions = "Stir",
            CocktailIngredients = new List<CocktailIngredient>
            {
                new()
                {
                    Id = 1, TypeId = 1, AmountValue = 60, AmountUnit = AmountUnit.Ml, IsOptional = false,
                    CocktailId = 1, Type = ginType
                },
                new()
                {
                    Id = 2, TypeId = 2, AmountValue = 2, AmountUnit = AmountUnit.Dash, IsOptional = true,
                    CocktailId = 1, Type = bitterType
                }
            }
        };

        await using (var context = new CocktailDbContext(options))
        {
            await context.AddRangeAsync(ginType, bitterType, bottle, cocktail);
            await context.SaveChangesAsync();
        }

        // Act
        Dictionary<int, AvailabilityResult> result;
        await using (var context = new CocktailDbContext(options))
        {
            var service = new AvailabilityService(context);
            result = await service.GetCocktailAvailabilityAsync();
        }

        // Assert
        Assert.True(result.ContainsKey(1));
        Assert.Equal(AvailabilityStatus.Available, result[1].Status);
        Assert.Empty(result[1].MissingRequired);
        Assert.Single(result[1].MissingOptional);
    }

    [Fact]
    public async Task GetCocktailAvailabilityAsync_WithMultipleCocktails_ReturnsAllResults()
    {
        // Arrange
        var options = CreateInMemoryOptions(
            nameof(GetCocktailAvailabilityAsync_WithMultipleCocktails_ReturnsAllResults));

        var ginType = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var rumType = new IngredientType { Id = 2, Name = "Rum", IsTracked = true };

        var bottle = new Bottle { Id = 1, FillLevel = FillLevel.Half, Name = "Tanqueray", TypeId = 1, Type = ginType };
        ginType.Bottles = new List<Bottle> { bottle };

        var martini = new Cocktail
        {
            Id = 1,
            Name = "Martini",
            Instructions = "Stir",
            CocktailIngredients = new List<CocktailIngredient>
            {
                new()
                {
                    Id = 1, TypeId = 1, AmountValue = 60, AmountUnit = AmountUnit.Ml, IsOptional = false,
                    CocktailId = 1, Type = ginType
                }
            }
        };

        var daiquiri = new Cocktail
        {
            Id = 2,
            Name = "Daiquiri",
            Instructions = "Shake",
            CocktailIngredients = new List<CocktailIngredient>
            {
                new()
                {
                    Id = 2, TypeId = 2, AmountValue = 60, AmountUnit = AmountUnit.Ml, IsOptional = false,
                    CocktailId = 2, Type = rumType
                }
            }
        };

        await using (var context = new CocktailDbContext(options))
        {
            await context.AddRangeAsync(ginType, rumType, bottle, martini, daiquiri);
            await context.SaveChangesAsync();
        }

        // Act
        Dictionary<int, AvailabilityResult> result;
        await using (var context = new CocktailDbContext(options))
        {
            var service = new AvailabilityService(context);
            result = await service.GetCocktailAvailabilityAsync();
        }

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, kvp => kvp is { Key: 1, Value.Status: AvailabilityStatus.Available });
        Assert.Contains(result, kvp => kvp is { Key: 2, Value.Status: AvailabilityStatus.Unavailable });
    }

    [Fact]
    public async Task GetCocktailAvailabilityAsync_WithEmptyDatabase_ReturnsEmptyDictionary()
    {
        // Arrange
        var options = CreateInMemoryOptions(
            nameof(GetCocktailAvailabilityAsync_WithEmptyDatabase_ReturnsEmptyDictionary));

        // Act
        await using var context = new CocktailDbContext(options);
        var service = new AvailabilityService(context);
        var result = await service.GetCocktailAvailabilityAsync();

        // Assert
        Assert.Empty(result);
    }
}
