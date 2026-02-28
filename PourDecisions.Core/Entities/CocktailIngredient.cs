using PourDecisions.Core.Enums;

namespace PourDecisions.Core.Entities;

public class CocktailIngredient
{
    public int Id { get; set; }
    public decimal AmountValue { get; set; }
    public AmountUnit AmountUnit { get; set; }
    public bool IsOptional { get; set; }

    public int CocktailId { get; set; }
    public required Cocktail Cocktail { get; set; }

    public int TypeId { get; set; }
    public required IngredientType Type { get; set; }
}