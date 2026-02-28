namespace PourDecisions.Core.Entities;

public class Cocktail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public bool IsFavorite { get; set; }

    public ICollection<CocktailIngredient> CocktailIngredients { get; set; } = [];
}