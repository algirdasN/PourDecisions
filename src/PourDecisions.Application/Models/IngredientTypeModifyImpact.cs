namespace PourDecisions.Application.Models;

public record IngredientTypeModifyImpact
{
    public List<string> CocktailNames { get; init; } = [];
    public List<BottleSummary> BottleInfoList { get; init; } = [];
}
