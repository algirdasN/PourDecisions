using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using PourDecisions.Desktop.Models;

namespace PourDecisions.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceProvider? _services;

    [ObservableProperty]
    private object _currentPage = null!;

    [ObservableProperty]
    private NavigationItem _selectedNavItem;

    public MainWindowViewModel(IServiceProvider? services)
    {
        _services = services;

        NavigationItems =
        [
            new NavigationItem("Cocktails", typeof(CocktailsViewModel)),
            new NavigationItem("My bar", typeof(InventoryViewModel)),
            new NavigationItem("Edit cocktails", typeof(EditCocktailsViewModel)),
            new NavigationItem("Settings", typeof(SettingsViewModel))
        ];

        SelectedNavItem = NavigationItems[0];
    }

    public string Title => "Pour Decisions";
    public ObservableCollection<NavigationItem> NavigationItems { get; }

    partial void OnSelectedNavItemChanged(NavigationItem value)
    {
        if (_services is null)
        {
            return;
        }

        CurrentPage = _services.GetRequiredService(value.ViewModelType);

        if (CurrentPage is IAsyncLoadable loadable)
        {
            _ = loadable.LoadAsync();
        }
    }
}
