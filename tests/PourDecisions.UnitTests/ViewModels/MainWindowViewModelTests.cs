using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PourDecisions.Desktop.Models;
using PourDecisions.Desktop.ViewModels;
using Xunit;

namespace PourDecisions.UnitTests.ViewModels;

public class MainWindowViewModelTests
{
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    [Fact]
    public void InitialState_IsCorrect()
    {
        // Arrange
        var cocktailsVm = Substitute.For<IAsyncLoadable>();
        _serviceProvider.GetService(typeof(CocktailsViewModel)).Returns(cocktailsVm);

        // Act
        var viewModel = new MainWindowViewModel(_serviceProvider);

        // Assert
        Assert.Equal("Pour Decisions", viewModel.Title);
        Assert.NotEmpty(viewModel.NavigationItems);
        Assert.Equal(viewModel.NavigationItems[0], viewModel.SelectedNavItem);
        Assert.Equal(cocktailsVm, viewModel.CurrentPage);
    }

    [Fact]
    public void ChangingSelectedNavItem_UpdatesCurrentPage_AndCallsLoadAsync()
    {
        // Arrange
        var cocktailsVm = Substitute.For<IAsyncLoadable>();
        var inventoryVm = Substitute.For<IAsyncLoadable>();
        _serviceProvider.GetService(typeof(CocktailsViewModel)).Returns(cocktailsVm);
        _serviceProvider.GetService(typeof(InventoryViewModel)).Returns(inventoryVm);
        
        var viewModel = new MainWindowViewModel(_serviceProvider);
        var inventoryNavItem = viewModel.NavigationItems.First(i => i.ViewModelType == typeof(InventoryViewModel));

        // Act
        viewModel.SelectedNavItem = inventoryNavItem;

        // Assert
        Assert.Equal(inventoryVm, viewModel.CurrentPage);
        inventoryVm.Received(1).LoadAsync();
    }
}
