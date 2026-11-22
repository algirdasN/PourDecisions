namespace PourDecisions.Application.Models;

public abstract record MergeIngredientResult
{
    public sealed record Success(bool IsTargetTracked) : MergeIngredientResult;
    public sealed record RecipeConflict(IList<string> CocktailNames) : MergeIngredientResult;
    public sealed record Error(string ErrorMessage) : MergeIngredientResult;
}
