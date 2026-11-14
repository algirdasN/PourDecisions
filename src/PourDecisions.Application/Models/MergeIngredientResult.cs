namespace PourDecisions.Application.Models;

public abstract record MergeIngredientResult
{
    public sealed record Success() : MergeIngredientResult;
    public sealed record Collision(IList<string> CocktailNames) : MergeIngredientResult;
    public sealed record Error(string ErrorMessage) : MergeIngredientResult;
}
