using PourDecisions.Desktop.ViewModels;

namespace PourDecisions.UnitTests.ViewModels;

public class IngredientFilterViewModelTests
{
    [Fact]
    public void Selecting_RaisesSelectionChanged()
    {
        var vm = new IngredientFilterViewModel(1, "Vodka");
        var invoked = false;
        vm.SelectionChanged += () => invoked = true;

        vm.IsSelected = true;

        Assert.True(invoked);
    }
}

