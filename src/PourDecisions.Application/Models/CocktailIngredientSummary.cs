using PourDecisions.Core.Enums;

namespace PourDecisions.Application.Models;

public record CocktailIngredientSummary(int Amount, AmountUnit Unit, string Name);
