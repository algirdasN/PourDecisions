using System.ComponentModel.DataAnnotations;

namespace PourDecisions.Desktop.Validators;

public static class IntegerValidator
{
    public static ValidationResult? ValidatePositive(string value, string errorMessage)
    {
        if (!int.TryParse(value, out var volume) || volume <= 0)
        {
            return new ValidationResult(errorMessage);
        }

        return ValidationResult.Success;
    }
}
