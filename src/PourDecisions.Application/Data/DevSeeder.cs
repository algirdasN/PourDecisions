using PourDecisions.Core.Data;
using PourDecisions.Core.Entities;
using PourDecisions.Core.Enums;

namespace PourDecisions.Application.Data;

public static class DevSeeder
{
    public static void Seed(CocktailDbContext db)
    {
        if (db.Cocktails.Any())
        {
            return; // idempotent - only seeds once
        }

        var dryGin = new IngredientType { Name = "Dry Gin", IsTracked = true };
        var dryVermouth = new IngredientType { Name = "Dry Vermouth", IsTracked = true };
        var olive = new IngredientType { Name = "Olive", IsTracked = false }; // untracked
        var tonic = new IngredientType { Name = "Tonic", IsTracked = false }; // untracked

        var tanqueray = new Ingredient { Name = "Tanqueray", Type = dryGin };
        var noilly = new Ingredient { Name = "Noilly Prat", Type = dryVermouth };

        var bottle1 = new Bottle { Ingredient = tanqueray, FillLevel = FillLevel.Full };
        // intentionally no vermouth bottle - Martini should show "Missing 1"

        var martini = new Cocktail
        {
            Name = "Dry Martini",
            IsFavorite = true,
            Instructions = "Stir with ice, strain into chilled glass.",
            CocktailIngredients = new List<CocktailIngredient>
            {
                new() { Type = dryGin, AmountValue = 60, AmountUnit = AmountUnit.Ml },
                new() { Type = dryVermouth, AmountValue = 10, AmountUnit = AmountUnit.Ml },
                new() { Type = olive, AmountValue = 1, AmountUnit = AmountUnit.Piece, IsOptional = true }
            }
        };

        var gintonic = new Cocktail
        {
            Name = "Gin & Tonic",
            Instructions = "Build in glass with ice, stir gently.",
            CocktailIngredients = new List<CocktailIngredient>
            {
                new() { Type = dryGin, AmountValue = 50, AmountUnit = AmountUnit.Ml },
                new() { Type = tonic, AmountValue = 150, AmountUnit = AmountUnit.Ml }
            }
        };

        db.AddRange(dryGin, dryVermouth, olive, tanqueray, noilly, bottle1, martini, gintonic);
        db.SaveChanges();
    }
}
