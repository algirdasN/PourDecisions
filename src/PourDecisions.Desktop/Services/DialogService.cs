using FluentAvalonia.UI.Controls;

namespace PourDecisions.Desktop.Services;

public interface IDialogService
{
    Task<ContentDialogResult> ShowInformationDialogAsync(string title, string message);

    Task<ContentDialogResult> ShowConfirmationDialogAsync(string title, string message, string primaryButtonText,
        string closeButtonText);
}

public class DialogService : IDialogService
{
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
