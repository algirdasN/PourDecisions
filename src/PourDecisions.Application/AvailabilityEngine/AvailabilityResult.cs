using PourDecisions.Core.Entities;

namespace PourDecisions.Application.AvailabilityEngine;

public record AvailabilityResult(AvailabilityStatus Status, IList<CocktailIngredient> MissingIngredients);
