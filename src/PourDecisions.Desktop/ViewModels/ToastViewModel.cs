using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using PourDecisions.Desktop.Services;

namespace PourDecisions.Desktop.ViewModels;

public partial class ToastViewModel : ViewModelBase
{
    private CancellationTokenSource _cts = new();

    [ObservableProperty]
    public partial bool IsShowing { get; set; }

    [ObservableProperty]
    public partial string? Message { get; set; }

    [ObservableProperty]
    public partial FAInfoBarSeverity Severity { get; set; }

    public ToastViewModel(IToastService toastService)
    {
        toastService.OnShowCalled += Show;
    }

    private void Show(FAInfoBarSeverity severity, string message)
    {
        _cts.Cancel();
        _cts.Dispose();

        _cts = new CancellationTokenSource();
        _ = ShowAsync(severity, message, _cts.Token);
    }

    private async Task ShowAsync(FAInfoBarSeverity severity, string message, CancellationToken ct)
    {
        Severity = severity;
        Message = message;
        IsShowing = true;

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (!ct.IsCancellationRequested)
            {
                IsShowing = false;
            }
        });
    }
}