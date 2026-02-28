using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PourDecisions.Core.Data;

public class CocktailDbContextFactory : IDesignTimeDbContextFactory<CocktailDbContext>
{
    public CocktailDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CocktailDbContext>()
            .UseSqlite("Data Source=dev.db")
            .Options;

        return new CocktailDbContext(options);
    }
}