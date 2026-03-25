using CommunityToolkit.Mvvm.ComponentModel;
using PourDecisions.Application.AvailabilityEngine;
using PourDecisions.Core.Entities;
using PourDecisions.Desktop.Models;

namespace PourDecisions.Desktop.ViewModels;

public partial class CocktailSummaryViewModel(Cocktail cocktail, AvailabilityResult availabilityResult) : ViewModelBase
{
    private readonly int _id = cocktail.Id;

    [ObservableProperty]
    private bool _isFavorite = cocktail.IsFavorite;

    public string Name { get; } = cocktail.Name;
    public string Instructions { get; } = cocktail.Instructions;

    public List<IngredientAvailabilityInfo> Ingredients { get; } = cocktail.CocktailIngredients
        .Select(ci => new IngredientAvailabilityInfo(IngredientDisplayText(ci),
            availabilityResult.MissingIngredients.Any(missing => missing.TypeId == ci.TypeId)))
        .ToList();

    public AvailabilityStatus AvailabilityStatus { get; } = availabilityResult.Status;

    public string AvailabilityLabel { get; } = availabilityResult.Status switch
    {
        AvailabilityStatus.Available => "✔️ available",
        AvailabilityStatus.Unavailable => $"❌ missing {availabilityResult.MissingIngredients.Count}",
        _ => throw new ArgumentOutOfRangeException()
    };

    public event Action<int, bool>? FavoriteToggled;

    partial void OnIsFavoriteChanged(bool value)
    {
        FavoriteToggled?.Invoke(_id, value);
    }

    private static string IngredientDisplayText(CocktailIngredient ci)
    {
        return $"{ci.AmountValue} {ci.AmountUnit.ToString().ToLowerInvariant()} of {ci.Type.Name.ToLowerInvariant()}";
    }
}
