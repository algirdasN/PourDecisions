using PourDecisions.Core.Entities;

namespace PourDecisions.Application.AvailabilityEngine;

/// <summary>
/// Provides calculations for determining cocktail availability based on available ingredient types.
/// </summary>
internal static class AvailabilityCalculator
{
    /// <summary>
    /// Calculates the availability status of a cocktail based on the available ingredient types.
    /// </summary>
    /// <param name="cocktail">The cocktail to check availability for.</param>
    /// <param name="availableTypeIds">A set of available ingredient type IDs.</param>
    /// <returns>An <see cref="AvailabilityResult"/> containing the availability status and lists of missing ingredients.</returns>
    public static AvailabilityResult CalculateCocktailAvailability(Cocktail cocktail, ISet<int> availableTypeIds)
    {
        var missingIngredients = cocktail.CocktailIngredients
            .Where(ingredient => !availableTypeIds.Contains(ingredient.TypeId)).ToList();

        var status = missingIngredients.Count == 0
            ? AvailabilityStatus.Available
            : AvailabilityStatus.Unavailable;

        return new AvailabilityResult(status, missingIngredients);
    }
}
