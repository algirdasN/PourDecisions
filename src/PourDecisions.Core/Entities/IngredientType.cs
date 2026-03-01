namespace PourDecisions.Core.Entities;

public class IngredientType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsTrackedByDefault { get; set; }

    public ICollection<Ingredient> Ingredients { get; set; } = [];
    public ICollection<CocktailIngredient> CocktailIngredients { get; set; } = [];
}