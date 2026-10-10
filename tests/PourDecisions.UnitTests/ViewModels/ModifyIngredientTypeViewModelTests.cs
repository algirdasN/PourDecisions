using PourDecisions.Core.Entities;
using PourDecisions.Desktop.ViewModels;

namespace PourDecisions.UnitTests.ViewModels;

public class ModifyIngredientTypeViewModelTests
{
    [Fact]
    public void Constructor_InitializesPropertiesFromIngredientType()
    {
        var type = new IngredientType { Id = 42, Name = "Gin", IsTracked = true };

        var vm = new ModifyIngredientTypeViewModel(type);

        Assert.Equal(42, vm.Id);
        Assert.Equal("Gin", vm.Name);
        Assert.True(vm.IsTracked);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public void ToggleTrackedCommand_InvokesToggleTrackedButtonClickedEvent()
    {
        var type = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var vm = new ModifyIngredientTypeViewModel(type);

        ModifyIngredientTypeViewModel? sender = null;
        bool? newStatus = null;
        vm.ToggleTrackedButtonClicked += (s, val) =>
        {
            sender = s;
            newStatus = val;
        };

        vm.ToggleTrackedCommand.Execute(null);

        Assert.Same(vm, sender);
        Assert.False(newStatus);
    }

    [Fact]
    public void RenameCommand_InvokesRenameButtonClickedEvent()
    {
        var type = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var vm = new ModifyIngredientTypeViewModel(type);

        ModifyIngredientTypeViewModel? sender = null;
        vm.RenameButtonClicked += s => sender = s;

        vm.RenameCommand.Execute(null);

        Assert.Same(vm, sender);
    }

    [Fact]
    public void DeleteCommand_InvokesDeleteButtonClickedEvent()
    {
        var type = new IngredientType { Id = 1, Name = "Gin", IsTracked = true };
        var vm = new ModifyIngredientTypeViewModel(type);

        ModifyIngredientTypeViewModel? sender = null;
        vm.DeleteButtonClicked += s => sender = s;

        vm.DeleteCommand.Execute(null);

        Assert.Same(vm, sender);
    }

    [Fact]
    public void NameComparer_ComparesCaseInsensitively()
    {
        var vm1 = new ModifyIngredientTypeViewModel(new IngredientType { Id = 1, Name = "apple" });
        var vm2 = new ModifyIngredientTypeViewModel(new IngredientType { Id = 2, Name = "Banana" });
        var vm3 = new ModifyIngredientTypeViewModel(new IngredientType { Id = 3, Name = "APPLE" });

        Assert.True(ModifyIngredientTypeViewModel.NameComparer.Compare(vm1, vm2) < 0);
        Assert.True(ModifyIngredientTypeViewModel.NameComparer.Compare(vm2, vm1) > 0);
        Assert.Equal(0, ModifyIngredientTypeViewModel.NameComparer.Compare(vm1, vm3));
    }
}
