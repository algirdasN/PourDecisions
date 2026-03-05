using System;
using System.IO;
using System.Linq;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PourDecisions.Application.AvailabilityEngine;
using PourDecisions.Core.Data;
using PourDecisions.Desktop.ViewModels;
using PourDecisions.Desktop.Views;

namespace PourDecisions.Desktop;

public class App : Avalonia.Application
{
    public static IServiceProvider Services { get; set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CocktailDbContext>();
        db.Database.Migrate();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(Services)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "cocktails.db");
        services.AddDbContext<CocktailDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        services.AddScoped<IAvailabilityService, AvailabilityService>();

        services.AddTransient<MainWindowViewModel>();

        services.AddTransient<CocktailsViewModel>();
        services.AddTransient<EditCocktailsViewModel>();
        services.AddTransient<MyBarViewModel>();
        services.AddTransient<SettingsViewModel>();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}