using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PourDecisions.Application.AvailabilityEngine;
using PourDecisions.Application.Data;
using PourDecisions.Application.Services;
using PourDecisions.Core.Data;
using PourDecisions.Desktop.Services;
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

#if DEBUG
        DevSeeder.Seed(db);
#endif

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
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
        services.AddScoped<IBottleService, BottleService>();
        services.AddScoped<ICocktailService, CocktailService>();
        services.AddScoped<IDialogService, DialogService>();
        services.AddScoped<IIngredientService, IngredientService>();

        services.AddTransient<MainWindowViewModel>();

        services.AddTransient<CocktailsViewModel>();
        services.AddTransient<EditCocktailsViewModel>();
        services.AddTransient<InventoryViewModel>();
        services.AddTransient<SettingsViewModel>();
    }
}
