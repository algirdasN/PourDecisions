using PourDecisions.Core.Enums;
using PourDecisions.Desktop.ViewModels;

namespace PourDecisions.UnitTests.ViewModels;

public class AddBottleViewModelTests
{
    [Fact]
    public void Validation_Fails_WhenPropertiesEmpty()
    {
        // Arrange
        var viewModel = new AddBottleViewModel([], string.Empty);

        // Act
        viewModel.AddBottleCommand.Execute(null);

        // Assert
        Assert.True(viewModel.HasErrors);
        Assert.NotEmpty(viewModel.GetErrors(nameof(viewModel.BottleName)));
        Assert.NotEmpty(viewModel.GetErrors(nameof(viewModel.IngredientTypeName)));
        Assert.NotEmpty(viewModel.GetErrors(nameof(viewModel.VolumeText)));
    }

    [Fact]
    public void Validation_Fails_WhenTypeNameTooShort()
    {
        // Arrange
        var viewModel = new AddBottleViewModel([], "Gi");

        // Act
        viewModel.AddBottleCommand.Execute(null);

        // Assert
        Assert.True(viewModel.HasErrors);
        Assert.NotEmpty(viewModel.GetErrors(nameof(viewModel.IngredientTypeName)));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("0")]
    public void Validation_Fails_WhenVolumeInvalid(string volume)
    {
        // Arrange
        var viewModel = new AddBottleViewModel([], "Gin")
        {
            BottleName = "Tanqueray",
            VolumeText = volume
        };

        // Act
        viewModel.AddBottleCommand.Execute(null);

        // Assert
        Assert.True(viewModel.HasErrors);
        Assert.NotEmpty(viewModel.GetErrors(nameof(viewModel.VolumeText)));
    }

    [Fact]
    public void AddBottle_InvokesEvent_AndClearsProperties()
    {
        // Arrange
        var viewModel = new AddBottleViewModel([], "Gin")
        {
            BottleName = "Tanqueray",
            VolumeText = "700",
            FillLevel = FillLevel.Half
        };

        string? invokedTypeName = null;
        string? invokedBottleName = null;
        var invokedVolume = 0;
        var invokedFill = FillLevel.Full;

        viewModel.OnAddButtonClicked += (typeName, bottleName, volume, fill) =>
        {
            invokedTypeName = typeName;
            invokedBottleName = bottleName;
            invokedVolume = volume;
            invokedFill = fill;
        };

        // Act
        viewModel.AddBottleCommand.Execute(null);

        // Assert
        Assert.False(viewModel.HasErrors);
        Assert.Equal("Gin", invokedTypeName);
        Assert.Equal("Tanqueray", invokedBottleName);
        Assert.Equal(700, invokedVolume);
        Assert.Equal(FillLevel.Half, invokedFill);

        Assert.Equal(string.Empty, viewModel.BottleName);
        Assert.Equal(string.Empty, viewModel.IngredientTypeName);
        Assert.Equal(string.Empty, viewModel.VolumeText);
        Assert.Equal(FillLevel.Full, viewModel.FillLevel);
    }

    [Fact]
    public void Cancel_InvokesEvent()
    {
        // Arrange
        var viewModel = new AddBottleViewModel([], "Gin");
        var invoked = false;
        viewModel.OnCancelButtonClicked += () => invoked = true;

        // Act
        viewModel.CancelCommand.Execute(null);

        // Assert
        Assert.True(invoked);
    }
}
