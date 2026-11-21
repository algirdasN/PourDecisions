using System.Diagnostics.CodeAnalysis;

namespace PourDecisions.Application.Models;

public abstract record RenameIngredientResult
{
    public sealed record Success(string NewName) : RenameIngredientResult;
    public sealed record NameCollisionCanMerge(string NewName, int ConflictId) : RenameIngredientResult;
    public sealed record NameCollisionMergeBlocked(IList<string> CocktailNames) : RenameIngredientResult;
    public sealed record Error(string ErrorMessage) : RenameIngredientResult;
}
