using PourDecisions.Application.Models;

namespace PourDecisions.UnitTests.Models;

public class CocktailEditSummaryTests
{
    [Fact]
    public void NameComparer_ShouldSortNullIdFirst()
    {
        // Arrange
        var newCocktail = new CocktailEditSummary(null, "Zest", false);
        var existingCocktail = new CocktailEditSummary(1, "Aperitivo", false);

        // Act
        var result = CocktailEditSummary.NameComparer.Compare(newCocktail, existingCocktail);

        // Assert
        Assert.True(result < 0, "Null ID should come before non-null ID");
    }

    [Fact]
    public void NameComparer_ShouldSortByDisplayNameIfBothHaveIds()
    {
        // Arrange
        var cocktail1 = new CocktailEditSummary(1, "Zest", false);
        var cocktail2 = new CocktailEditSummary(2, "Aperitivo", false);

        // Act
        var result = CocktailEditSummary.NameComparer.Compare(cocktail1, cocktail2);

        // Assert
        Assert.True(result > 0, "'Zest' should come after 'Aperitivo'");
    }

    [Fact]
    public void NameComparer_ShouldSortByDisplayNameIfBothHaveNullIds()
    {
        // Arrange
        var cocktail1 = new CocktailEditSummary(null, "Zest", false);
        var cocktail2 = new CocktailEditSummary(null, "Aperitivo", false);

        // Act
        var result = CocktailEditSummary.NameComparer.Compare(cocktail1, cocktail2);

        // Assert
        Assert.True(result > 0, "'Zest' should come after 'Aperitivo'");
    }
}
