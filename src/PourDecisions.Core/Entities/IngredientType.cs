namespace PourDecisions.Core.Entities;

public class IngredientType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsTracked { get; set; }

    public ICollection<Bottle> Bottles { get; set; } = [];
    public ICollection<CocktailIngredient> CocktailIngredients { get; set; } = [];
}
