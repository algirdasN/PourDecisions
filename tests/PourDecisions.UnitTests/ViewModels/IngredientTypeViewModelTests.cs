using PourDecisions.Core.Entities;
using PourDecisions.Core.Enums;
using PourDecisions.Desktop.ViewModels;

namespace PourDecisions.UnitTests.ViewModels;

public class IngredientTypeViewModelTests
{
    [Fact]
    public void LoadBottles_CreatesBottleViewModels_SortedByName()
    {
        // Arrange
        var type = new IngredientType { Id = 1, Name = "Gin" };
        var bottles = new List<Bottle>
        {
            new() { Id = 1, Name = "Tanqueray", Type = type },
            new() { Id = 2, Name = "Beefeater", Type = type }
        };

        // Act
        var viewModel = new IngredientTypeViewModel(type, bottles);

        // Assert
        Assert.Equal(2, viewModel.Bottles.Count);
        Assert.Equal("Beefeater", viewModel.Bottles[0].Name);
        Assert.Equal("Tanqueray", viewModel.Bottles[1].Name);
        Assert.Equal("Gin (2)", viewModel.DisplayName);
    }

    [Fact]
    public void ChildEvents_AreForwarded()
    {
        // Arrange
        var type = new IngredientType { Id = 1, Name = "Gin" };
        var bottles = new List<Bottle>
        {
            new() { Id = 1, Name = "Tanqueray", Type = type }
        };
        var viewModel = new IngredientTypeViewModel(type, bottles);

        int? fillBottleId = null;
        FillLevel? newFill = null;
        viewModel.FillLevelChanged += (id, fill) =>
        {
            fillBottleId = id;
            newFill = fill;
        };

        int? deleteBottleId = null;
        string? deleteBottleName = null;
        int? deleteTypeId = null;
        string? deleteTypeName = null;
        viewModel.DeleteBottleClicked += (bid, bname, tid, tname) =>
        {
            deleteBottleId = bid;
            deleteBottleName = bname;
            deleteTypeId = tid;
            deleteTypeName = tname;
        };

        var bottleVm = viewModel.Bottles[0];

        // Act
        bottleVm.CycleFillLevelCommand.Execute(null);

        // Assert
        Assert.Equal(1, fillBottleId);
        // BottleViewModel cycles: Full -> Half -> Quarter -> Full
        Assert.Equal(FillLevel.Half, newFill);

        // Act
        bottleVm.DeleteBottleCommand.Execute(null);

        // Assert
        Assert.Equal(1, deleteBottleId);
        Assert.Equal("Tanqueray", deleteBottleName);
        Assert.Equal(1, deleteTypeId);
        Assert.Equal("Gin", deleteTypeName);
    }

    [Fact]
    public void AddForm_InvokesEvent()
    {
        // Arrange
        var type = new IngredientType { Id = 1, Name = "Gin" };
        var viewModel = new IngredientTypeViewModel(type, new List<Bottle>());
        string? invokedName = null;
        viewModel.AddFormClicked += name => invokedName = name;

        // Act
        viewModel.AddFormCommand.Execute(null);

        // Assert
        Assert.Equal("Gin", invokedName);
        Assert.True(viewModel.IsExpanded);
    }
}
