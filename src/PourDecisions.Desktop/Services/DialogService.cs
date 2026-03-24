using FluentAvalonia.UI.Controls;

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
    /// <returns>A <see cref="ContentDialogResult"/> representing the user's action.</returns>
    Task<ContentDialogResult> ShowInformationDialogAsync(string title, string message);

    /// <summary>
    /// Asynchronously displays a confirmation dialog with a title, a message, a primary action button, and a close button.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message describing the action to confirm.</param>
    /// <param name="primaryButtonText">The text for the primary action button.</param>
    /// <param name="closeButtonText">The text for the close or cancel button.</param>
    /// <returns>A <see cref="ContentDialogResult"/> representing the user's choice.</returns>
    Task<ContentDialogResult> ShowConfirmationDialogAsync(string title, string message, string primaryButtonText,
        string closeButtonText);
}

/// <summary>
/// Implements dialog operations using FluentAvalonia's <see cref="ContentDialog"/>.
/// </summary>
public class DialogService : IDialogService
{
    /// <inheritdoc/>
    public async Task<ContentDialogResult> ShowInformationDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "Close"
        };

        return await dialog.ShowAsync();
    }

    /// <inheritdoc/>
    public async Task<ContentDialogResult> ShowConfirmationDialogAsync(string title, string message,
        string primaryButtonText, string closeButtonText)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = closeButtonText
        };

        return await dialog.ShowAsync();
    }
}
