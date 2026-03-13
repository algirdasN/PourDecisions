using Microsoft.EntityFrameworkCore;
using PourDecisions.Core.Entities;

namespace PourDecisions.Core.Data;

public class CocktailDbContext(DbContextOptions<CocktailDbContext> options) : DbContext(options)
{
    public DbSet<IngredientType> IngredientTypes => Set<IngredientType>();
    public DbSet<Bottle> Bottles => Set<Bottle>();
    public DbSet<Cocktail> Cocktails => Set<Cocktail>();
    public DbSet<CocktailIngredient> CocktailIngredients => Set<CocktailIngredient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IngredientType>()
            .HasIndex(t => t.Name)
            .IsUnique();

        modelBuilder.Entity<Bottle>()
            .Property(b => b.FillLevel)
            .HasConversion<string>();

        modelBuilder.Entity<CocktailIngredient>()
            .Property(ci => ci.AmountUnit)
            .HasConversion<string>();
    }
}
