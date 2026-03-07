using PourDecisions.Core.Enums;

namespace PourDecisions.Core.Entities;

public class CocktailIngredient
{
    public int Id { get; set; }
    public int AmountValue { get; set; }
    public AmountUnit AmountUnit { get; set; }
    public bool IsOptional { get; set; }

    public int CocktailId { get; set; }
    public Cocktail Cocktail { get; set; } = null!;

    public int TypeId { get; set; }
    public IngredientType Type { get; set; } = null!;
}
