using System.ComponentModel.DataAnnotations;
using FluentAvalonia.UI.Controls;
using PourDecisions.Desktop.ViewModels;
using PourDecisions.Desktop.Views;

namespace PourDecisions.Desktop.Services;

/// <summary>
/// Provides an interface for displaying various types of dialogs to the user.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Asynchronously displays an information dialog with a title, a message, and a close button.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The informational message to display.</param>
    /// <returns>A <see cref="FAContentDialogResult"/> representing the user's action.</returns>
    Task<FAContentDialogResult> ShowInformationDialogAsync(string title, string message);

    /// <summary>
    /// Asynchronously displays a confirmation dialog with a title, a message, a primary action button, and a close button.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message describing the action to confirm.</param>
    /// <param name="primaryButtonText">The text for the primary action button.</param>
    /// <param name="closeButtonText">The text for the close or cancel button.</param>
    /// <returns>A <see cref="FAContentDialogResult"/> representing the user's choice.</returns>
    Task<FAContentDialogResult> ShowConfirmationDialogAsync(string title, string message, string primaryButtonText,
        string closeButtonText);

    /// <summary>
    /// Asynchronously displays an input dialog that allows the user to enter a value, with optional validation.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message displayed to the user in the dialog.</param>
    /// <param name="primaryButtonText">The text for the primary action button.</param>
    /// <param name="closeButtonText">The text for the close button.</param>
    /// <param name="initialValue">The initial value to populate the input field. Defaults to an empty string.</param>
    /// <param name="validator"> A function to validate the user's input. Defaults to null.</param>
    /// <returns>The value entered by the user, or null if the dialog was canceled.</returns>
    Task<string?> ShowInputDialogAsync(string title, string message, string primaryButtonText,
        string closeButtonText, string initialValue = "", Func<string, ValidationResult?>? validator = null);
}

/// <summary>
/// Implements dialog operations using FluentAvalonia's <see cref="FAContentDialog"/>.
/// </summary>
public class DialogService : IDialogService
{
    /// <inheritdoc/>
    public async Task<FAContentDialogResult> ShowInformationDialogAsync(string title, string message)
    {
        var dialog = new FAContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "Close",
            DefaultButton = FAContentDialogButton.Close
        };

        return await dialog.ShowAsync();
    }

    /// <inheritdoc/>
    public async Task<FAContentDialogResult> ShowConfirmationDialogAsync(string title, string message,
        string primaryButtonText, string closeButtonText)
    {
        var dialog = new FAContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = closeButtonText,
            DefaultButton = FAContentDialogButton.Primary
        };

        return await dialog.ShowAsync();
    }

    /// <inheritdoc/>
    public async Task<string?> ShowInputDialogAsync(string title, string message,
        string primaryButtonText, string closeButtonText, string initialValue = "",
        Func<string, ValidationResult?>? validator = null)
    {
        var vm = new InputDialogViewModel(message, initialValue, validator);

        var dialog = new FAContentDialog
        {
            Title = title,
            Content = new InputDialogView { DataContext = vm },
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = closeButtonText,
            DefaultButton = FAContentDialogButton.Primary
        };

        dialog.PrimaryButtonClick += (sender, args) =>
        {
            if (vm.HasValidationError())
            {
                args.Cancel = true;
            }
        };

        return await dialog.ShowAsync() == FAContentDialogResult.Primary
            ? vm.Value
            : null;
    }
}
