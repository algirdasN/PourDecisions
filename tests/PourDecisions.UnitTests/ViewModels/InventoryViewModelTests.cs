using FluentAvalonia.UI.Controls;
using NSubstitute;
using PourDecisions.Application.Services;
using PourDecisions.Core.Entities;
using PourDecisions.Core.Enums;
using PourDecisions.Desktop.Services;
using PourDecisions.Desktop.ViewModels;

namespace PourDecisions.UnitTests.ViewModels;

public class InventoryViewModelTests
{
    private readonly IBottleService _bottleService = Substitute.For<IBottleService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly IIngredientService _ingredientService = Substitute.For<IIngredientService>();

    [Fact]
    public async Task LoadAsync_LoadsIngredientTypes_SortedByName()
    {
        // Arrange
        var type1 = new IngredientType { Id = 1, Name = "Gin" };
        var type2 = new IngredientType { Id = 2, Name = "Bourbon" };
        var bottle1 = new Bottle { Id = 1, Name = "Tanqueray", Type = type1 };
        var bottle2 = new Bottle { Id = 2, Name = "Buffalo Trace", Type = type2 };

        _bottleService.GetBottlesWithTypeAsync().Returns(new List<Bottle> { bottle1, bottle2 });
        _ingredientService.GetTrackedIngredientTypeNamesAsync().Returns(new List<string> { "Gin", "Bourbon" });

        var viewModel = new InventoryViewModel(_bottleService, _dialogService, _ingredientService);

        // Act
        await viewModel.LoadAsync();

        // Assert
        Assert.Equal(2, viewModel.IngredientTypes.Count);
        Assert.Equal("Bourbon", viewModel.IngredientTypes[0].Name);
        Assert.Equal("Gin", viewModel.IngredientTypes[1].Name);
        Assert.False(viewModel.IsEmpty);
    }

    [Fact]
    public async Task AddBottle_UpdatesList_WhenNewType()
    {
        // Arrange
        _bottleService.GetBottlesWithTypeAsync().Returns(new List<Bottle>());
        _ingredientService.GetTrackedIngredientTypeNamesAsync().Returns(new List<string>());

        var viewModel = new InventoryViewModel(_bottleService, _dialogService, _ingredientService);
        await viewModel.LoadAsync();

        var newType = new IngredientType { Id = 1, Name = "Gin" };
        var newBottle = new Bottle { Id = 1, Name = "Tanqueray", Type = newType };
        _bottleService.AddBottleAsync("Gin", "Tanqueray", 700, FillLevel.Full).Returns(newBottle);
        _bottleService.GetBottlesOfTypeAsync(1).Returns(new List<Bottle> { newBottle });

        // Act
        viewModel.AddFormCommand.Execute(null);
        Assert.True(viewModel.IsAddingBottle);
        Assert.NotNull(viewModel.AddBottleForm);

        viewModel.AddBottleForm.BottleName = "Tanqueray";
        viewModel.AddBottleForm.IngredientTypeName = "Gin";
        viewModel.AddBottleForm.VolumeText = "700";
        viewModel.AddBottleForm.AddBottleCommand.Execute(null);

        // Small delay to allow a fire-and-forget task to complete
        await Task.Delay(100);

        // Assert
        Assert.Single(viewModel.IngredientTypes);
        Assert.Equal("Gin", viewModel.IngredientTypes[0].Name);
        Assert.Single(viewModel.IngredientTypes[0].Bottles);
    }

    [Fact]
    public async Task DeleteBottle_RemovesType_WhenLastBottle()
    {
        // Arrange
        var type = new IngredientType { Id = 1, Name = "Gin" };
        var bottle = new Bottle { Id = 1, Name = "Tanqueray", Type = type };
        _bottleService.GetBottlesWithTypeAsync().Returns(new List<Bottle> { bottle });
        _ingredientService.GetTrackedIngredientTypeNamesAsync().Returns(new List<string> { "Gin" });
        _dialogService
            .ShowConfirmationDialogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(ContentDialogResult.Primary);
        _bottleService.GetBottlesOfTypeAsync(1).Returns(new List<Bottle>());

        var viewModel = new InventoryViewModel(_bottleService, _dialogService, _ingredientService);
        await viewModel.LoadAsync();

        // Act
        // Trigger deletion through the child view model
        viewModel.IngredientTypes[0].Bottles[0].DeleteBottleCommand.Execute(null);

        await Task.Delay(100);

        // Assert
        Assert.Empty(viewModel.IngredientTypes);
        Assert.True(viewModel.IsEmpty);
    }

    [Fact]
    public async Task OnFillLevelChanged_CallsService()
    {
        // Arrange
        var type = new IngredientType { Id = 1, Name = "Gin" };
        var bottle = new Bottle { Id = 1, Name = "Tanqueray", Type = type, FillLevel = FillLevel.Full };
        _bottleService.GetBottlesWithTypeAsync().Returns(new List<Bottle> { bottle });
        _ingredientService.GetTrackedIngredientTypeNamesAsync().Returns(new List<string> { "Gin" });

        var viewModel = new InventoryViewModel(_bottleService, _dialogService, _ingredientService);
        await viewModel.LoadAsync();

        // Act
        viewModel.IngredientTypes[0].Bottles[0].CycleFillLevelCommand.Execute(null);

        await Task.Delay(100);

        // Assert
        await _bottleService.Received(1).UpdateBottleFillLevelAsync(1, FillLevel.Half);
    }
}
