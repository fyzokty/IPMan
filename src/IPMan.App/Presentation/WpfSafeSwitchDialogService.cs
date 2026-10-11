using System.Windows;
using System.Windows.Threading;
using IPMan.App.Services;
using IPMan.App.ViewModels;
using IPMan.App.Views;

namespace IPMan.App.Presentation;

/// <summary>Hosts safety dialogs on the UI dispatcher, including switches initiated from the tray.</summary>
public sealed class WpfSafeSwitchDialogService(Dispatcher dispatcher, WindowsThemeService theme,
    Func<InternetLossConfirmationViewModel, InternetLossConfirmationWindow> confirmationFactory,
    Func<SafetyCountdownViewModel, SafetyCountdownWindow> countdownFactory,
    Func<SafeSwitchExitWindow> exitFactory) : IInternetLossConfirmationService, ISafetyCountdownDialogService
{
    private SafetyCountdownWindow? _countdownWindow;

    public async Task<InternetLossConfirmationResult> ConfirmAsync(InternetLossConfirmationViewModel viewModel,
        CancellationToken cancellationToken)
    {
        return await dispatcher.InvokeAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            InternetLossConfirmationWindow window = confirmationFactory(viewModel);
            Prepare(window);
            using CancellationTokenRegistration registration = cancellationToken.Register(() =>
                dispatcher.BeginInvoke(window.Close));
            bool accepted = window.ShowDialog() == true;
            cancellationToken.ThrowIfCancellationRequested();
            return new InternetLossConfirmationResult(accepted, viewModel.DoNotAskAgain);
        });
    }

    public async Task ShowAsync(SafetyCountdownViewModel viewModel)
    {
        await dispatcher.InvokeAsync(() =>
        {
            if (viewModel.Decision.IsCompleted)
            {
                return;
            }
            SafetyCountdownWindow window = countdownFactory(viewModel);
            Prepare(window);
            _countdownWindow = window;
            Task close = CloseAfterDecisionAsync(window, viewModel);
            try
            {
                window.ShowDialog();
            }
            finally
            {
                viewModel.Complete(SafetyCountdownDecision.Revert);
                _countdownWindow = null;
            }
            _ = close;
        });
    }

    public async Task<SafeSwitchExitDecision> ConfirmExitAsync(CancellationToken cancellationToken)
    {
        return await dispatcher.InvokeAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            SafeSwitchExitWindow window = exitFactory();
            Prepare(window);
            if (_countdownWindow is { IsVisible: true } countdown)
            {
                window.Owner = countdown;
            }
            using CancellationTokenRegistration registration = cancellationToken.Register(() =>
                dispatcher.BeginInvoke(window.Close));
            window.ShowDialog();
            cancellationToken.ThrowIfCancellationRequested();
            return window.Decision;
        });
    }

    private async Task CloseAfterDecisionAsync(Window window, SafetyCountdownViewModel viewModel)
    {
        await viewModel.Decision;
        await dispatcher.InvokeAsync(window.Close);
    }

    private void Prepare(Window window)
    {
        theme.AttachWindow(window);
        if (System.Windows.Application.Current?.MainWindow is { IsVisible: true } owner)
        {
            window.Owner = owner;
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }
}
