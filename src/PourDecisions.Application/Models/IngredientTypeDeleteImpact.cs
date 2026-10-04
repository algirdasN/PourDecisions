namespace PourDecisions.Application.Models;

public record IngredientTypeDeleteImpact
{
    public List<string> CocktailNames { get; init; } = [];
    public List<string> BottleInfoList { get; init; } = [];
}
