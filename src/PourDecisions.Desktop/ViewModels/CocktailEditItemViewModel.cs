using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PourDecisions.Application.Models;
using PourDecisions.Core.Entities;
using PourDecisions.Core.Enums;
using PourDecisions.Desktop.Models;
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
    [NotifyCanExecuteChangedFor(nameof(UndoFormCommand))]
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

        Ingredients[0].IsFirst = true;
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

        WeakReferenceMessenger.Default.Register<IngredientValidationChangedMessage>(this,
            (_, m) =>
            {
                _errorMap[m.Sender] = (m.AmountError, m.NameError);
                RefreshErrors();
            });

        WeakReferenceMessenger.Default.Register<DuplicateNameCheckMessage>(this, (_, _) =>
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
        });
    }

    public string Header { get; }
    public string SaveButtonLabel => _id is null ? "Add" : "Edit";
    public bool CanDeleteCocktail => _id is not null;
    public string LongestAmountUnit { get; } = Enum.GetNames<AmountUnit>().MaxBy(x => x.Length)!;

    public event Action<int?, string, IList<CocktailIngredientSummary>, string>? SaveCocktailClicked;
    public event Action<int?>? OnUndoChangesClicked;
    public event Action<int, string>? OnDeleteCocktailClicked;

    [RelayCommand]
    private void AddIngredient()
    {
        Ingredients.Add(CreateIngredientViewModel(null));
    }

    [RelayCommand]
    private void SaveCocktail()
    {
        foreach (var ingredient in Ingredients)
        {
            ingredient.ValidateAllProperties();
        }

        if (_errorMap.Any(kvp => kvp.Value.AmountError is not null || kvp.Value.NameError is not null))
        {
            return;
        }

        var ingredientData = Ingredients
            .Select(ingredient => ingredient.GetIngredientData())
            .ToList();

        SaveCocktailClicked?.Invoke(_id, Name, ingredientData, Instructions);
    }

    [RelayCommand(CanExecute = nameof(IsDirty))]
    private void UndoForm()
    {
        OnUndoChangesClicked?.Invoke(_id);
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

    private void OnNavigationIconClicked(CocktailIngredientViewModel cocktailIngredient)
    {
        var index = Ingredients.IndexOf(cocktailIngredient);
        var newIndex = index == 0 ? 1 : index - 1;
        Ingredients.Move(index, newIndex);
    }

    private void OnDeleteClicked(CocktailIngredientViewModel cocktailIngredient)
    {
        cocktailIngredient.OnChanged -= SetDirty;
        cocktailIngredient.OnNavigationIconClicked -= OnNavigationIconClicked;
        cocktailIngredient.OnDeleteClicked -= OnDeleteClicked;

        Ingredients.Remove(cocktailIngredient);
        if (Ingredients.Count == 0)
        {
            Ingredients.Add(CreateIngredientViewModel(null));
        }

        _errorMap.Remove(cocktailIngredient);
        RefreshErrors();
    }

    private CocktailIngredientViewModel CreateIngredientViewModel(CocktailIngredient? cocktailIngredient)
    {
        var vm = new CocktailIngredientViewModel(cocktailIngredient);
        vm.OnChanged += SetDirty;
        vm.OnNavigationIconClicked += OnNavigationIconClicked;
        vm.OnDeleteClicked += OnDeleteClicked;
        return vm;
    }

    private void RefreshErrors()
    {
        AmountError = _errorMap.FirstOrDefault(kvp => kvp.Value.AmountError is not null).Value.AmountError;
        NameError = _errorMap.FirstOrDefault(kvp => kvp.Value.NameError is not null).Value.NameError;
    }

    partial void OnNameChanged(string value)
    {
        SetDirty();
    }

    partial void OnInstructionsChanged(string value)
    {
        SetDirty();
    }

    private void OnIngredientChanged()
    {
        SetDirty();
    }

    private void SetDirty()
    {
        IsDirty = true;
    }
}
