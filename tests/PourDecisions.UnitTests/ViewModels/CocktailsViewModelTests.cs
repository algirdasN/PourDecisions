using NSubstitute;
using PourDecisions.Application.AvailabilityEngine;
using PourDecisions.Application.Services;
using PourDecisions.Core.Entities;
using PourDecisions.Desktop.Services;
using PourDecisions.Desktop.ViewModels;

namespace PourDecisions.UnitTests.ViewModels;

public class CocktailsViewModelTests
{
    private readonly IAvailabilityService _availabilityService = Substitute.For<IAvailabilityService>();
    private readonly ICocktailService _cocktailService = Substitute.For<ICocktailService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();

    [Fact]
    public async Task LoadAsync_LoadsCocktails_AndSetsSelected()
    {
        // Arrange
        var cocktail = new Cocktail { Id = 1, Name = "Martini", CocktailIngredients = new List<CocktailIngredient>() };
        _cocktailService.GetAllWithIngredientsAsync().Returns(new List<Cocktail> { cocktail });
        _availabilityService.GetCocktailAvailabilityAsync().Returns(new Dictionary<int, AvailabilityResult>
        {
            {
                1,
                new AvailabilityResult(AvailabilityStatus.Available, new List<CocktailIngredient>())
            }
        });

        var viewModel = new CocktailsViewModel(_availabilityService, _cocktailService, _dialogService);

        // Act
        await viewModel.LoadAsync();

        // Assert
        Assert.Single(viewModel.FilteredCocktails);
        Assert.Equal("Martini", viewModel.FilteredCocktails[0].Name);
        Assert.Equal(viewModel.FilteredCocktails[0], viewModel.SelectedCocktail);
        Assert.True(viewModel.HasResults);
    }

    [Fact]
    public async Task Filter_BySearchText_Works()
    {
        // Arrange
        var cocktail1 = new Cocktail { Id = 1, Name = "Martini", CocktailIngredients = new List<CocktailIngredient>() };
        var cocktail2 = new Cocktail
            { Id = 2, Name = "Margarita", CocktailIngredients = new List<CocktailIngredient>() };
        _cocktailService.GetAllWithIngredientsAsync().Returns(new List<Cocktail> { cocktail1, cocktail2 });
        _availabilityService.GetCocktailAvailabilityAsync().Returns(new Dictionary<int, AvailabilityResult>
        {
            {
                1,
                new AvailabilityResult(AvailabilityStatus.Available, new List<CocktailIngredient>())
            },
            {
                2,
                new AvailabilityResult(AvailabilityStatus.Available, new List<CocktailIngredient>())
            }
        });

        var viewModel = new CocktailsViewModel(_availabilityService, _cocktailService, _dialogService);
        await viewModel.LoadAsync();

        // Act
        viewModel.NameSearchText = "MarT"; // Case-insensitive

        // Assert
        Assert.Single(viewModel.FilteredCocktails);
        Assert.Equal("Martini", viewModel.FilteredCocktails[0].Name);
    }

    [Fact]
    public async Task Filter_ByAvailableOnly_Works()
    {
        // Arrange
        var cocktail1 = new Cocktail
            { Id = 1, Name = "Available", CocktailIngredients = new List<CocktailIngredient>() };
        var cocktail2 = new Cocktail
            { Id = 2, Name = "Unavailable", CocktailIngredients = new List<CocktailIngredient>() };
        _cocktailService.GetAllWithIngredientsAsync().Returns(new List<Cocktail> { cocktail1, cocktail2 });
        _availabilityService.GetCocktailAvailabilityAsync().Returns(new Dictionary<int, AvailabilityResult>
        {
            {
                1,
                new AvailabilityResult(AvailabilityStatus.Available, new List<CocktailIngredient>())
            },
            {
                2,
                new AvailabilityResult(AvailabilityStatus.Unavailable, new List<CocktailIngredient> { new() })
            }
        });

        var viewModel = new CocktailsViewModel(_availabilityService, _cocktailService, _dialogService);
        await viewModel.LoadAsync();

        // Act
        viewModel.ShowAvailableOnly = true;

        // Assert
        Assert.Single(viewModel.FilteredCocktails);
        Assert.Equal("Available", viewModel.FilteredCocktails[0].Name);
    }

    [Fact]
    public async Task Filter_ByFavoriteOnly_Works()
    {
        // Arrange
        var cocktail1 = new Cocktail
            { Id = 1, Name = "Favorite", IsFavorite = true, CocktailIngredients = new List<CocktailIngredient>() };
        var cocktail2 = new Cocktail
            { Id = 2, Name = "Not Favorite", IsFavorite = false, CocktailIngredients = new List<CocktailIngredient>() };
        _cocktailService.GetAllWithIngredientsAsync().Returns(new List<Cocktail> { cocktail1, cocktail2 });
        _availabilityService.GetCocktailAvailabilityAsync().Returns(new Dictionary<int, AvailabilityResult>
        {
            {
                1,
                new AvailabilityResult(AvailabilityStatus.Available, new List<CocktailIngredient>())
            },
            {
                2,
                new AvailabilityResult(AvailabilityStatus.Available, new List<CocktailIngredient>())
            }
        });

        var viewModel = new CocktailsViewModel(_availabilityService, _cocktailService, _dialogService);
        await viewModel.LoadAsync();

        // Act
        viewModel.ShowFavoriteOnly = true;

        // Assert
        Assert.Single(viewModel.FilteredCocktails);
        Assert.Equal("Favorite", viewModel.FilteredCocktails[0].Name);
    }

    [Fact]
    public async Task ClearFilters_ResetsAllFilters()
    {
        // Arrange
        var viewModel = new CocktailsViewModel(_availabilityService, _cocktailService, _dialogService)
        {
            NameSearchText = "something",
            ShowAvailableOnly = true,
            ShowFavoriteOnly = true
        };

        // Act
        viewModel.ClearFiltersCommand.Execute(null);

        // Assert
        Assert.Equal(string.Empty, viewModel.NameSearchText);
        Assert.False(viewModel.ShowAvailableOnly);
        Assert.False(viewModel.ShowFavoriteOnly);
    }

    [Fact]
    public async Task OnFavoriteToggled_CallsServiceAndRefilters()
    {
        // Arrange
        var cocktail1 = new Cocktail
            { Id = 1, Name = "Cocktail", IsFavorite = false, CocktailIngredients = new List<CocktailIngredient>() };
        _cocktailService.GetAllWithIngredientsAsync().Returns(new List<Cocktail> { cocktail1 });
        _availabilityService.GetCocktailAvailabilityAsync().Returns(new Dictionary<int, AvailabilityResult>
        {
            {
                1,
                new AvailabilityResult(AvailabilityStatus.Available, new List<CocktailIngredient>())
            }
        });

        var viewModel = new CocktailsViewModel(_availabilityService, _cocktailService, _dialogService);
        await viewModel.LoadAsync();
        var cocktailVm = viewModel.FilteredCocktails[0];
        viewModel.ShowFavoriteOnly = true;

        // Assert cocktail is filtered out
        Assert.Empty(viewModel.FilteredCocktails);

        // Act
        cocktailVm.IsFavorite = true;

        // Assert
        await _cocktailService.Received(1).SetFavoriteAsync(1, true);
        // Should be back in a filtered list because it's now a favorite
        Assert.Single(viewModel.FilteredCocktails);
        Assert.Equal(cocktailVm, viewModel.FilteredCocktails[0]);

        // Act toggle off
        cocktailVm.IsFavorite = false;

        // Assert
        await _cocktailService.Received(1).SetFavoriteAsync(1, false);
        // Should be empty now because ShowFavoriteOnly is true
        Assert.Empty(viewModel.FilteredCocktails);
    }
}
