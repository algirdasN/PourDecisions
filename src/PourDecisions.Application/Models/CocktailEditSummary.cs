namespace PourDecisions.Application.Models;

public record CocktailEditSummary(int? Id, string Name, bool IsFavorite)
{
    public static readonly Comparer<CocktailEditSummary> NameComparer =
        Comparer<CocktailEditSummary>.Create((x, y) =>
            x.Id.HasValue.CompareTo(y.Id.HasValue) switch
            {
                0 => string.Compare(x.Name, y.Name, StringComparison.Ordinal),
                var idComp => idComp
            });
};
