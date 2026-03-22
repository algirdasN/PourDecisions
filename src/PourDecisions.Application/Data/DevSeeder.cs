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

        // Spirits
        var dryGin = new IngredientType { Name = "Dry Gin", IsTracked = true };
        var vodka = new IngredientType { Name = "Vodka", IsTracked = true };
        var rum = new IngredientType { Name = "Rum", IsTracked = true };
        var tequila = new IngredientType { Name = "Tequila", IsTracked = true };
        var whiskey = new IngredientType { Name = "Whiskey", IsTracked = true };
        var brandy = new IngredientType { Name = "Brandy", IsTracked = true };

        // Fortified wines
        var dryVermouth = new IngredientType { Name = "Dry Vermouth", IsTracked = true };
        var sweetVermouth = new IngredientType { Name = "Sweet Vermouth", IsTracked = true };

        // Mixers (untracked)
        var tonic = new IngredientType { Name = "Tonic", IsTracked = false };
        var cola = new IngredientType { Name = "Cola", IsTracked = false };
        var limejuice = new IngredientType { Name = "Lime Juice", IsTracked = false };
        var lemonjuice = new IngredientType { Name = "Lemon Juice", IsTracked = false };
        var simplesyrup = new IngredientType { Name = "Simple Syrup", IsTracked = false };

        // Garnishes (untracked)
        var olive = new IngredientType { Name = "Olive", IsTracked = false };
        var lemontwist = new IngredientType { Name = "Lemon Twist", IsTracked = false };
        var lime = new IngredientType { Name = "Lime", IsTracked = false };
        var cherry = new IngredientType { Name = "Cherry", IsTracked = false };

        // Bottles
        var bottle1 = new Bottle { Name = "Tanqueray", Type = dryGin, Volume = 750, FillLevel = FillLevel.Full };
        var bottle2 = new Bottle { Name = "Beefeater", Type = dryGin, Volume = 500, FillLevel = FillLevel.Half };
        var bottle3 = new Bottle { Name = "Gordon's", Type = dryGin, Volume = 1000, FillLevel = FillLevel.Quarter };
        var bottle4 = new Bottle { Name = "Ketel One", Type = vodka, Volume = 700, FillLevel = FillLevel.Full };
        var bottle5 = new Bottle { Name = "Grey Goose", Type = vodka, Volume = 750, FillLevel = FillLevel.Half };
        var bottle6 = new Bottle { Name = "Bacardi", Type = rum, Volume = 500, FillLevel = FillLevel.Full };
        var bottle7 = new Bottle { Name = "Captain Morgan", Type = rum, Volume = 1000, FillLevel = FillLevel.Half };
        var bottle8 = new Bottle { Name = "Jose Cuervo", Type = tequila, Volume = 750, FillLevel = FillLevel.Quarter };
        var bottle9 = new Bottle { Name = "Patrón", Type = tequila, Volume = 700, FillLevel = FillLevel.Half };
        var bottle10 = new Bottle { Name = "Jack Daniel's", Type = whiskey, Volume = 500, FillLevel = FillLevel.Full };
        var bottle11 = new Bottle { Name = "Jameson", Type = whiskey, Volume = 1000, FillLevel = FillLevel.Half };
        var bottle12 = new Bottle { Name = "Courvoisier", Type = brandy, Volume = 750, FillLevel = FillLevel.Quarter };
        var bottle13 = new Bottle { Name = "Dolin", Type = sweetVermouth, Volume = 500, FillLevel = FillLevel.Full };

        var martini = new Cocktail
        {
            Name = "Dry Martini",
            IsFavorite = true,
            Instructions = "Stir with ice, strain into chilled glass.",
            CocktailIngredients = new List<CocktailIngredient>
            {
                new() { Type = dryGin, AmountValue = 60, AmountUnit = AmountUnit.Ml },
                new() { Type = dryVermouth, AmountValue = 10, AmountUnit = AmountUnit.Ml },
                new() { Type = olive, AmountValue = 1, AmountUnit = AmountUnit.Piece }
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

        db.AddRange(
            // Spirits
            dryGin, vodka, rum, tequila, whiskey, brandy,
            // Fortified wines
            dryVermouth, sweetVermouth,
            // Mixers
            tonic, cola, limejuice, lemonjuice, simplesyrup,
            // Garnishes
            olive, lemontwist, lime, cherry,
            // Bottles (15 total, with multiples of same types and varying volumes)
            bottle1, bottle2, bottle3, bottle4, bottle5, bottle6, bottle7, bottle8, bottle9, bottle10, bottle11,
            bottle12, bottle13,
            // Cocktails
            martini, gintonic
        );
        db.SaveChanges();
    }
}
