using PourDecisions.Core.Enums;

namespace PourDecisions.Core.Entities;

public class Bottle
{
    public int Id { get; set; }
    public FillLevel FillLevel { get; set; }

    public int IngredientId { get; set; }
    public required Ingredient Ingredient { get; set; }
}