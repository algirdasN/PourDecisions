using FluentAvalonia.UI.Controls;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PourDecisions.Application.Models;
using PourDecisions.Application.Services;
using PourDecisions.Core.Entities;
using PourDecisions.Desktop.Services;
using PourDecisions.Desktop.ViewModels;

namespace PourDecisions.UnitTests.ViewModels;

public class SettingsViewModelTests
{
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly IIngredientService _ingredientService = Substitute.For<IIngredientService>();

    [Fact]
    public async Task LoadAsync_PopulatesIngredientTypes()
    {
        // Arrange
        var types = new List<IngredientType>
        {
            new() { Id = 1, Name = "Gin", IsTracked = true },
            new() { Id = 2, Name = "Vodka", IsTracked = false }
        };
        _ingredientService.GetIngredientTypesAsync().Returns(types);

        var viewModel = new SettingsViewModel(_ingredientService, _dialogService);

        // Act
        await viewModel.LoadAsync();

        // Assert
        Assert.Equal(2, viewModel.IngredientTypes.Count);
        Assert.Equal("Gin", viewModel.IngredientTypes[0].Name);
        Assert.True(viewModel.IngredientTypes[0].IsTracked);
        Assert.Equal("Vodka", viewModel.IngredientTypes[1].Name);
        Assert.False(viewModel.IngredientTypes[1].IsTracked);
    }

    [Fact]
    public async Task ToggleTracked_ToTrue_UpdatesServiceAndProperty()
    {
        // Arrange
        var types = new List<IngredientType>
        {
            new() { Id = 1, Name = "Gin", IsTracked = false }
        };
        _ingredientService.GetIngredientTypesAsync().Returns(types);

        var viewModel = new SettingsViewModel(_ingredientService, _dialogService);
        await viewModel.LoadAsync();

        // Act
        viewModel.IngredientTypes[0].ToggleTrackedCommand.Execute(null);
        await Task.Delay(50);

        // Assert
        await _ingredientService.Received(1).UpdateIngredientTypeTrackedAsync(1, true, true);
        Assert.True(viewModel.IngredientTypes[0].IsTracked);
    }

    [Fact]
    public async Task ToggleTracked_ToFalse_NoBottles_UpdatesDirectly()
    {
        // Arrange
        var types = new List<IngredientType>
        {
            new() { Id = 1, Name = "Gin", IsTracked = true }
        };
        _ingredientService.GetIngredientTypesAsync().Returns(types);
        _ingredientService.PreviewIngredientTypeModifyAsync(1).Returns(new IngredientTypeModifyImpact
        {
            CocktailNames = [],
            BottleInfoList = []
        });

        var viewModel = new SettingsViewModel(_ingredientService, _dialogService);
        await viewModel.LoadAsync();

        // Act
        viewModel.IngredientTypes[0].ToggleTrackedCommand.Execute(null);
        await Task.Delay(50);

        // Assert
        await _ingredientService.Received(1).UpdateIngredientTypeTrackedAsync(1, false, true);
        Assert.False(viewModel.IngredientTypes[0].IsTracked);
    }

    [Fact]
    public async Task ToggleTracked_ToFalse_WithBottles_Confirmed_UpdatesAndDeletesBottles()
    {
        // Arrange
        var types = new List<IngredientType>
        {
            new() { Id = 1, Name = "Gin", IsTracked = true }
        };
        _ingredientService.GetIngredientTypesAsync().Returns(types);
        _ingredientService.PreviewIngredientTypeModifyAsync(1).Returns(new IngredientTypeModifyImpact
        {
            CocktailNames = [],
            BottleInfoList = [new BottleSummary("Bombay", 700)]
        });
        _dialogService.ShowConfirmationDialogAsync(Arg.Any<string>(), Arg.Any<string>(), "Confirm", "Cancel",
            Arg.Any<FAContentDialogButton>()).Returns(FAContentDialogResult.Primary);

        var viewModel = new SettingsViewModel(_ingredientService, _dialogService);
        await viewModel.LoadAsync();

        // Act
        viewModel.IngredientTypes[0].ToggleTrackedCommand.Execute(null);
        await Task.Delay(50);

        // Assert
        await _ingredientService.Received(1).UpdateIngredientTypeTrackedAsync(1, false, true);
        Assert.False(viewModel.IngredientTypes[0].IsTracked);
    }

    [Fact]
    public async Task ToggleTracked_ToFalse_WithBottles_Cancelled_DoesNotUpdate()
    {
        // Arrange
        var types = new List<IngredientType>
        {
            new() { Id = 1, Name = "Gin", IsTracked = true }
        };
        _ingredientService.GetIngredientTypesAsync().Returns(types);
        _ingredientService.PreviewIngredientTypeModifyAsync(1).Returns(new IngredientTypeModifyImpact
        {
            CocktailNames = [],
            BottleInfoList = [new BottleSummary("Bombay", 700)]
        });
        _dialogService.ShowConfirmationDialogAsync(Arg.Any<string>(), Arg.Any<string>(), "Confirm", "Cancel",
            Arg.Any<FAContentDialogButton>()).Returns(FAContentDialogResult.Secondary);

        var viewModel = new SettingsViewModel(_ingredientService, _dialogService);
        await viewModel.LoadAsync();

        // Act
        viewModel.IngredientTypes[0].ToggleTrackedCommand.Execute(null);
        await Task.Delay(50);

        // Assert
        await _ingredientService.DidNotReceive()
            .UpdateIngredientTypeTrackedAsync(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<bool>());
        Assert.True(viewModel.IngredientTypes[0].IsTracked);
    }

    [Fact]
    public async Task ToggleTracked_ServiceThrows_ShowsErrorDialog()
    {
        // Arrange
        var types = new List<IngredientType>
        {
            new() { Id = 1, Name = "Gin", IsTracked = false }
        };
        _ingredientService.GetIngredientTypesAsync().Returns(types);
        _ingredientService.UpdateIngredientTypeTrackedAsync(1, true, true)
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var viewModel = new SettingsViewModel(_ingredientService, _dialogService);
        await viewModel.LoadAsync();

        // Act
        viewModel.IngredientTypes[0].ToggleTrackedCommand.Execute(null);
        await Task.Delay(50);

        // Assert
        await _dialogService.Received(1).ShowInformationDialogAsync("Failed to change tracked status", "DB error");
        Assert.False(viewModel.IngredientTypes[0].IsBusy);
    }

    [Fact]
    public async Task Rename_Success_UpdatesNameAndResortsList()
    {
        // Arrange
        var types = new List<IngredientType>
        {
            new() { Id = 1, Name = "Gin", IsTracked = true },
            new() { Id = 2, Name = "Vodka", IsTracked = true }
        };
        _ingredientService.GetIngredientTypesAsync().Returns(types);
        _ingredientService.RenameIngredientAsync(1, "Whiskey").Returns(Result<string>.Success("Whiskey"));

        _dialogService.ShowInputDialogAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<Func<string, Task<Result>>>())
            .Returns(async callInfo =>
            {
                var callback = callInfo.ArgAt<Func<string, Task<Result>>>(5);
                if (callback != null)
                {
                    await callback("Whiskey");
                }

                return (string?)"Whiskey";
            });

        var viewModel = new SettingsViewModel(_ingredientService, _dialogService);
        await viewModel.LoadAsync();

        // Act
        viewModel.IngredientTypes[0].RenameCommand.Execute(null);
        await Task.Delay(50);

        // Assert
        Assert.Equal("Whiskey", viewModel.IngredientTypes[1].Name);
        Assert.Equal("Vodka", viewModel.IngredientTypes[0].Name);
        Assert.False(viewModel.IngredientTypes[1].IsBusy);
    }

    [Fact]
    public async Task Delete_UsedInCocktails_ShowsInformationDialogAndDoesNotDelete()
    {
        // Arrange
        var types = new List<IngredientType>
        {
            new() { Id = 1, Name = "Gin", IsTracked = true }
        };
        _ingredientService.GetIngredientTypesAsync().Returns(types);
        _ingredientService.PreviewIngredientTypeModifyAsync(1).Returns(new IngredientTypeModifyImpact
        {
            CocktailNames = ["Martini"],
            BottleInfoList = []
        });

        var viewModel = new SettingsViewModel(_ingredientService, _dialogService);
        await viewModel.LoadAsync();

        // Act
        viewModel.IngredientTypes[0].DeleteCommand.Execute(null);
        await Task.Delay(50);

        // Assert
        await _dialogService.Received(1).ShowInformationDialogAsync(
            "Cannot delete ingredient type",
            Arg.Is<string>(msg => msg.Contains("Martini")));
        await _ingredientService.DidNotReceive().DeleteIngredientTypeAsync(Arg.Any<int>(), Arg.Any<bool>());
        Assert.Single(viewModel.IngredientTypes);
    }

    [Fact]
    public async Task Delete_NotUsedInCocktails_Confirmed_DeletesAndRemovesFromList()
    {
        // Arrange
        var types = new List<IngredientType>
        {
            new() { Id = 1, Name = "Gin", IsTracked = true },
            new() { Id = 2, Name = "Vodka", IsTracked = true }
        };
        _ingredientService.GetIngredientTypesAsync().Returns(types);
        _ingredientService.PreviewIngredientTypeModifyAsync(1).Returns(new IngredientTypeModifyImpact
        {
            CocktailNames = [],
            BottleInfoList = [new BottleSummary("Bombay", 700)]
        });
        _dialogService.ShowConfirmationDialogAsync("Delete ingredient type", Arg.Any<string>(), "Delete", "Cancel",
            Arg.Any<FAContentDialogButton>()).Returns(FAContentDialogResult.Primary);

        var viewModel = new SettingsViewModel(_ingredientService, _dialogService);
        await viewModel.LoadAsync();

        // Act
        viewModel.IngredientTypes[0].DeleteCommand.Execute(null);
        await Task.Delay(50);

        // Assert
        await _ingredientService.Received(1).DeleteIngredientTypeAsync(1, true);
        Assert.Single(viewModel.IngredientTypes);
        Assert.Equal("Vodka", viewModel.IngredientTypes[0].Name);
    }

    [Fact]
    public async Task Delete_NotUsedInCocktails_Cancelled_DoesNotDelete()
    {
        // Arrange
        var types = new List<IngredientType>
        {
            new() { Id = 1, Name = "Gin", IsTracked = true }
        };
        _ingredientService.GetIngredientTypesAsync().Returns(types);
        _ingredientService.PreviewIngredientTypeModifyAsync(1).Returns(new IngredientTypeModifyImpact
        {
            CocktailNames = [],
            BottleInfoList = []
        });
        _dialogService.ShowConfirmationDialogAsync("Delete ingredient type", Arg.Any<string>(), "Delete", "Cancel",
            Arg.Any<FAContentDialogButton>()).Returns(FAContentDialogResult.Secondary);

        var viewModel = new SettingsViewModel(_ingredientService, _dialogService);
        await viewModel.LoadAsync();

        // Act
        viewModel.IngredientTypes[0].DeleteCommand.Execute(null);
        await Task.Delay(50);

        // Assert
        await _ingredientService.DidNotReceive().DeleteIngredientTypeAsync(Arg.Any<int>(), Arg.Any<bool>());
        Assert.Single(viewModel.IngredientTypes);
    }
}
