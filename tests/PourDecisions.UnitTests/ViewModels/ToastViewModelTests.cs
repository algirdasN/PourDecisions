using FluentAvalonia.UI.Controls;
using PourDecisions.Desktop.Services;
using PourDecisions.Desktop.ViewModels;

namespace PourDecisions.UnitTests.ViewModels;

public class ToastViewModelTests
{
    [Fact]
    public void Show_ReplacesMessageAndSeverity()
    {
        var service = new ToastService();
        var viewModel = new ToastViewModel(service);

        service.ShowSuccess("Added");
        Assert.True(viewModel.IsShowing);
        Assert.Equal("Added", viewModel.Message);
        Assert.Equal(FAInfoBarSeverity.Success, viewModel.Severity);

        service.ShowWarning("Deleted");
        Assert.True(viewModel.IsShowing);
        Assert.Equal("Deleted", viewModel.Message);
        Assert.Equal(FAInfoBarSeverity.Warning, viewModel.Severity);
    }
}