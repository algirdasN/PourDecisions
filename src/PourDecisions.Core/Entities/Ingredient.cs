namespace PourDecisions.Core.Entities;

public class Ingredient
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int TypeId { get; set; }
    public required IngredientType Type { get; set; }

    public ICollection<Bottle> Bottles { get; set; } = [];
}