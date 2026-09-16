using PourDecisions.Core.Enums;

namespace PourDecisions.Core.Entities;

public class Bottle
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Volume { get; set; }
    public FillLevel FillLevel { get; set; }

    public int TypeId { get; set; }
    public required IngredientType Type { get; set; }
}
