namespace PourDecisions.Desktop.Models;

public record IngredientValidationChangedMessage(object Sender, string? AmountError, string? NameError);
