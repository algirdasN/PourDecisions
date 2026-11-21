using FluentAvalonia.UI.Controls;

namespace PourDecisions.Desktop.Services;

/// <summary>Publishes requests to display toast notifications.</summary>
public interface IToastService
{
    /// <summary>
    /// Raised synchronously on the calling thread with the notification severity and message.
    /// </summary>
    event Action<FAInfoBarSeverity, string>? OnShowCalled;

    /// <summary>Requests a success notification.</summary>
    /// <param name="message">The message to display.</param>
    void ShowSuccess(string message);

    /// <summary>Requests a warning notification.</summary>
    /// <param name="message">The message to display.</param>
    void ShowWarning(string message);
}

/// <summary>Publishes toast requests to subscribers of <see cref="OnShowCalled"/>.</summary>
public class ToastService : IToastService
{
    /// <inheritdoc />
    public event Action<FAInfoBarSeverity, string>? OnShowCalled;

    /// <inheritdoc />
    public void ShowSuccess(string message)
    {
        OnShowCalled?.Invoke(FAInfoBarSeverity.Success, message);
    }

    /// <inheritdoc />
    public void ShowWarning(string message)
    {
        OnShowCalled?.Invoke(FAInfoBarSeverity.Warning, message);
    }
}