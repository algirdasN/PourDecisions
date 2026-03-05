using PourDecisions.Core.Entities;

namespace PourDecisions.Application.AvailabilityEngine;

internal static class AvailabilityCalculator
{
    public static AvailabilityResult CalculateCocktailAvailability(Cocktail cocktail, ISet<int> availableTypeIds)
    {
        var missingRequired = new List<CocktailIngredient>();
        var missingOptional = new List<CocktailIngredient>();

        foreach (var ingredient in cocktail.CocktailIngredients)
        {
            if (availableTypeIds.Contains(ingredient.TypeId))
            {
                continue;
            }

            if (ingredient.IsOptional)
            {
                missingOptional.Add(ingredient);
            }
            else
            {
                missingRequired.Add(ingredient);
            }
        }

        var status = missingRequired.Count == 0
            ? AvailabilityStatus.Available
            : AvailabilityStatus.Unavailable;

        return new AvailabilityResult(status, missingRequired, missingOptional);
    }
}