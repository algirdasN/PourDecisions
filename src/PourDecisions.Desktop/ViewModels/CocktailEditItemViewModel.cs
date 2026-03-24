using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PourDecisions.Application.Models;
using PourDecisions.Core.Entities;
using PourDecisions.Core.Enums;
using PourDecisions.Shared.Extensions;

namespace PourDecisions.Desktop.ViewModels;

public partial class CocktailEditItemViewModel : ViewModelBase
{
    private readonly Dictionary<object, (string? AmountError, string? NameError)> _errorMap = new();

    private readonly int? _id;

    [ObservableProperty]
    private string? _amountError;

    [ObservableProperty]
    private ObservableCollection<CocktailIngredientViewModel> _ingredients;

    [ObservableProperty]
    private ObservableCollection<string> _ingredientTypeNames;

    [ObservableProperty]
    private string _instructions;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCocktailCommand))]
    private bool _isDirty;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string? _nameError;

    public CocktailEditItemViewModel(ObservableCollection<string> ingredientTypeNames, Cocktail? cocktail = null)
    {
        _ingredientTypeNames = ingredientTypeNames;

        _ingredients = cocktail is null
            ? [CreateIngredientViewModel(null)]
            : cocktail.CocktailIngredients
                .Select(CreateIngredientViewModel)
                .ToObservableCollection();

        Ingredients.FirstOrDefault()?.IsFirst = true;
        Ingredients.CollectionChanged += (_, _) =>
        {
            for (var i = 0; i < Ingredients.Count; i++)
            {
                Ingredients[i].IsFirst = i == 0;
            }
        };

        _id = cocktail?.Id;
        _instructions = cocktail?.Instructions ?? string.Empty;
        _name = cocktail?.Name ?? string.Empty;
        Header = cocktail is null ? "Add new cocktail" : $"Edit cocktail: '{cocktail.Name}'";
        _isDirty = false;
    }

    public string Header { get; }
    public string SaveButtonLabel => _id is null ? "Add" : "Save";
    public bool CanDeleteCocktail => _id is not null;
    public string LongestAmountUnit { get; } = Enum.GetNames<AmountUnit>().MaxBy(x => x.Length)!;

    public event Action<int?, string, IList<CocktailIngredientSummary>, string>? SaveCocktailClicked;
    public event Action<int, string>? OnDeleteCocktailClicked;

    [RelayCommand]
    private void AddIngredient()
    {
        Ingredients.Add(CreateIngredientViewModel(null));
    }

    [RelayCommand(CanExecute = nameof(IsDirty))]
    private void SaveCocktail()
    {
        foreach (var ingredient in Ingredients)
        {
            ingredient.TriggerValidation();
        }

        if (_errorMap.Any(kvp => kvp.Value.AmountError is not null || kvp.Value.NameError is not null))
        {
            return;
        }

        var ingredientData = Ingredients
            .Select(ingredient => ingredient.GetIngredientData())
            .ToList();

        SaveCocktailClicked?.Invoke(_id, Name, ingredientData, Instructions);
        IsDirty = false;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteCocktail))]
    private void DeleteCocktail()
    {
        if (_id is null)
        {
            return;
        }

        OnDeleteCocktailClicked?.Invoke(_id.Value, Name);
    }

    private void OnNavigationIconClicked(CocktailIngredientViewModel cocktailIngredient, bool moveDown)
    {
        var index = Ingredients.IndexOf(cocktailIngredient);
        var newIndex = moveDown ? index + 1 : index - 1;
        Ingredients.Move(index, newIndex);
    }

    private void OnDeleteClicked(CocktailIngredientViewModel cocktailIngredient)
    {
        cocktailIngredient.OnChanged -= SetDirty;
        cocktailIngredient.OnNavigationIconClicked -= OnNavigationIconClicked;
        cocktailIngredient.OnDeleteClicked -= OnDeleteClicked;
        cocktailIngredient.OnValidationChanged -= OnValidationChanged;
        cocktailIngredient.OnDuplicateCheckNeeded -= OnDuplicateCheckNeeded;

        Ingredients.Remove(cocktailIngredient);
        if (Ingredients.Count == 0)
        {
            Ingredients.Add(CreateIngredientViewModel(null));
        }

        _errorMap.Remove(cocktailIngredient);
        RefreshErrors();
    }

    private void OnValidationChanged(object sender, string? amountError, string? nameError)
    {
        _errorMap[sender] = (amountError, nameError);
        RefreshErrors();
    }

    private void OnDuplicateCheckNeeded()
    {
        var duplicates = Ingredients
            .GroupBy(ingredient => ingredient.Name.ToLower())
            .Where(group => !string.IsNullOrEmpty(group.Key) && group.Count() > 1)
            .SelectMany(g => g)
            .ToList();

        foreach (var ingredient in Ingredients)
        {
            ingredient.HasDuplicateName = duplicates.Contains(ingredient);
        }
    }

    private CocktailIngredientViewModel CreateIngredientViewModel(CocktailIngredient? cocktailIngredient)
    {
        var vm = new CocktailIngredientViewModel(cocktailIngredient);
        vm.OnChanged += SetDirty;
        vm.OnNavigationIconClicked += OnNavigationIconClicked;
        vm.OnDeleteClicked += OnDeleteClicked;
        vm.OnValidationChanged += OnValidationChanged;
        vm.OnDuplicateCheckNeeded += OnDuplicateCheckNeeded;
        return vm;
    }

    private void RefreshErrors()
    {
        AmountError = _errorMap.FirstOrDefault(kvp => kvp.Value.AmountError is not null).Value.AmountError;
        NameError = _errorMap.FirstOrDefault(kvp => kvp.Value.NameError is not null).Value.NameError;
    }

    private void SetDirty()
    {
        IsDirty = true;
    }

    partial void OnNameChanged(string value)
    {
        SetDirty();
    }

    partial void OnInstructionsChanged(string value)
    {
        SetDirty();
    }
}
