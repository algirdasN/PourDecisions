using PourDecisions.Desktop.ViewModels;

namespace PourDecisions.UnitTests.ViewModels;

public class InputDialogViewModelTests
{
    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        var message = "Enter name:";
        var initialValue = "Initial";

        var vm = new InputDialogViewModel(message, initialValue);

        Assert.Equal(message, vm.Message);
        Assert.Equal(initialValue, vm.Value);
        Assert.Null(vm.ErrorMessage);
        Assert.False(vm.HasError);
    }

    [Fact]
    public void Constructor_WithDefaultInitialValue_SetsEmptyValue()
    {
        var vm = new InputDialogViewModel("Prompt");

        Assert.Equal(string.Empty, vm.Value);
        Assert.Null(vm.ErrorMessage);
        Assert.False(vm.HasError);
    }

    [Fact]
    public void ErrorMessage_WhenSet_UpdatesHasError()
    {
        var vm = new InputDialogViewModel("Prompt");

        vm.ErrorMessage = "Invalid input";

        Assert.Equal("Invalid input", vm.ErrorMessage);
        Assert.True(vm.HasError);
    }

    [Fact]
    public void ChangingValue_ClearsErrorMessageAndHasError()
    {
        var vm = new InputDialogViewModel("Prompt", "Old");
        vm.ErrorMessage = "Some error";
        Assert.True(vm.HasError);

        vm.Value = "New Value";

        Assert.Null(vm.ErrorMessage);
        Assert.False(vm.HasError);
    }
}
