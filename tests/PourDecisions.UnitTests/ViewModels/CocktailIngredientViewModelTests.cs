using PourDecisions.Core.Entities;
using PourDecisions.Core.Enums;
using PourDecisions.Desktop.ViewModels;

namespace PourDecisions.UnitTests.ViewModels;

public class CocktailIngredientViewModelTests
{
    [Fact]
    public void Constructor_WithNull_SetsDefaultValues()
    {
        // Act
        var viewModel = new CocktailIngredientViewModel(null);

        // Assert
        Assert.Equal(string.Empty, viewModel.AmountText);
        Assert.Equal(string.Empty, viewModel.Name);
        Assert.Equal(AmountUnit.Ml, viewModel.Unit);
    }

    [Fact]
    public void Constructor_WithCocktailIngredient_SetsValues()
    {
        // Arrange
        var ingredient = new CocktailIngredient
        {
            AmountValue = 50,
            AmountUnit = AmountUnit.Dash,
            Type = new IngredientType { Name = "Gin" }
        };

        // Act
        var viewModel = new CocktailIngredientViewModel(ingredient);

        // Assert
        Assert.Equal("50", viewModel.AmountText);
        Assert.Equal("Gin", viewModel.Name);
        Assert.Equal(AmountUnit.Dash, viewModel.Unit);
    }

    [Fact]
    public void GetIngredientData_ValidData_ReturnsSummary()
    {
        // Arrange
        var viewModel = new CocktailIngredientViewModel
        {
            AmountText = "60",
            Name = "Rum",
            Unit = AmountUnit.Ml
        };

        // Act
        var summary = viewModel.GetIngredientData();

        // Assert
        Assert.Equal(60, summary.Amount);
        Assert.Equal("Rum", summary.Name);
        Assert.Equal(AmountUnit.Ml, summary.Unit);
    }

    [Fact]
    public void GetIngredientData_InvalidAmount_ThrowsException()
    {
        // Arrange
        var viewModel = new CocktailIngredientViewModel
        {
            AmountText = "abc"
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => viewModel.GetIngredientData());
    }

    [Fact]
    public void TriggerValidation_WithInvalidData_HasErrors()
    {
        // Arrange
        var viewModel = new CocktailIngredientViewModel
        {
            AmountText = "-1",
            Name = "A" // Too short (min length 3)
        };

        // Act
        viewModel.TriggerValidation();

        // Assert
        Assert.True(viewModel.HasErrors);
        var amountErrors = viewModel.GetErrors(nameof(viewModel.AmountText));
        Assert.Contains(amountErrors, e => e.ErrorMessage == "Amount must be a positive integer");
        
        var nameErrors = viewModel.GetErrors(nameof(viewModel.Name));
        Assert.Contains(nameErrors, e => e.ErrorMessage == "Type name must be at least 3 characters");
    }

    [Fact]
    public void OnChanged_IsInvoked_WhenPropertiesChange()
    {
        // Arrange
        var viewModel = new CocktailIngredientViewModel();
        var changedInvoked = false;
        viewModel.Changed += () => changedInvoked = true;

        // Act
        viewModel.AmountText = "10";

        // Assert
        Assert.True(changedInvoked);

        // Reset
        changedInvoked = false;
        viewModel.Name = "New Name";
        Assert.True(changedInvoked);

        // Reset
        changedInvoked = false;
        viewModel.Unit = AmountUnit.Dash;
        Assert.True(changedInvoked);
    }

    [Fact]
    public void NavigateCommand_InvokesEvents()
    {
        // Arrange
        var viewModel = new CocktailIngredientViewModel { IsFirst = true };
        var changedInvoked = false;
        var navigationInvoked = false;
        viewModel.Changed += () => changedInvoked = true;
        viewModel.NavigationIconClicked += (vm, isFirst) => navigationInvoked = true;

        // Act
        viewModel.NavigateCommand.Execute(null);

        // Assert
        Assert.True(changedInvoked);
        Assert.True(navigationInvoked);
    }

    [Fact]
    public void DeleteCommand_InvokesEvents()
    {
        // Arrange
        var viewModel = new CocktailIngredientViewModel();
        var changedInvoked = false;
        var deleteInvoked = false;
        var duplicateCheckInvoked = false;
        viewModel.Changed += () => changedInvoked = true;
        viewModel.DeleteClicked += (vm) => deleteInvoked = true;
        viewModel.DuplicateCheckNeeded += () => duplicateCheckInvoked = true;

        // Act
        viewModel.DeleteCommand.Execute(null);

        // Assert
        Assert.True(changedInvoked);
        Assert.True(deleteInvoked);
        Assert.True(duplicateCheckInvoked);
    }

    [Fact]
    public void HasDuplicateName_TriggersValidationError()
    {
        // Arrange
        var viewModel = new CocktailIngredientViewModel { Name = "Vodka" };
        
        // Act
        viewModel.HasDuplicateName = true;

        // Assert
        Assert.True(viewModel.HasErrors);
        var errors = viewModel.GetErrors(nameof(viewModel.Name));
        Assert.Contains(errors, e => e.ErrorMessage == "Ingredient names must be unique");
    }
}
