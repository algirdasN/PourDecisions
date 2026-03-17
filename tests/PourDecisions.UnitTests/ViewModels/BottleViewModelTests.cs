using PourDecisions.Core.Entities;
using PourDecisions.Core.Enums;
using PourDecisions.Desktop.ViewModels;

namespace PourDecisions.UnitTests.ViewModels;

public class BottleViewModelTests
{
    [Fact]
    public void Properties_Match_BottleEntity()
    {
        // Arrange
        var bottle = new Bottle
        {
            Id = 1,
            Name = "Tanqueray",
            Volume = 700,
            FillLevel = FillLevel.Half,
            Type = new IngredientType { Name = "Gin" }
        };

        // Act
        var viewModel = new BottleViewModel(bottle);

        // Assert
        Assert.Equal(bottle.Name, viewModel.Name);
        Assert.Equal("700 ml", viewModel.Volume);
        Assert.Equal(FillLevel.Half, viewModel.Fill);
        Assert.False(string.IsNullOrEmpty(viewModel.FillDisplay));
    }

    [Fact]
    public void CycleFillLevel_ChangesFillLevel_AndInvokesEvent()
    {
        // Arrange
        var bottle = new Bottle
        {
            Id = 1,
            Name = "Tanqueray",
            Volume = 700,
            FillLevel = FillLevel.Full,
            Type = new IngredientType { Name = "Gin" }
        };
        var viewModel = new BottleViewModel(bottle);
        int? invokedId = null;
        FillLevel? invokedFill = null;
        viewModel.FillLevelChanged += (id, fill) =>
        {
            invokedId = id;
            invokedFill = fill;
        };

        // Act
        viewModel.CycleFillLevelCommand.Execute(null);

        // Assert
        Assert.Equal(FillLevel.Half, viewModel.Fill);
        Assert.Equal(1, invokedId);
        Assert.Equal(FillLevel.Half, invokedFill);

        // Cycle again
        viewModel.CycleFillLevelCommand.Execute(null);
        Assert.Equal(FillLevel.Quarter, viewModel.Fill);
        Assert.Equal(FillLevel.Quarter, invokedFill);

        // Cycle back to Full
        viewModel.CycleFillLevelCommand.Execute(null);
        Assert.Equal(FillLevel.Full, viewModel.Fill);
        Assert.Equal(FillLevel.Full, invokedFill);
    }

    [Fact]
    public void DeleteBottle_InvokesEvent()
    {
        // Arrange
        var bottle = new Bottle
        {
            Id = 1,
            Name = "Tanqueray",
            Volume = 700,
            FillLevel = FillLevel.Full,
            Type = new IngredientType { Name = "Gin" }
        };
        var viewModel = new BottleViewModel(bottle);
        int? invokedId = null;
        string? invokedName = null;
        viewModel.DeleteBottleClicked += (id, name) =>
        {
            invokedId = id;
            invokedName = name;
        };

        // Act
        viewModel.DeleteBottleCommand.Execute(null);

        // Assert
        Assert.Equal(1, invokedId);
        Assert.Equal("Tanqueray", invokedName);
    }
}
