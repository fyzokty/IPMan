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
        SafetyCountdownWindow? window = null;
        try
        {
            await dispatcher.InvokeAsync(() =>
            {
                if (viewModel.Decision.IsCompleted)
                {
                    return;
                }
                window = countdownFactory(viewModel);
                theme.AttachWindow(window);
                // An independent topmost window stays visible when the shell hides to the tray.
                // AdapterActionsViewModel keeps mutations disabled until this task completes.
                DialogWindowPlacement.Prepare(window, null);
                _countdownWindow = window;
                window.Show();
            });
            await viewModel.Decision;
        }
        finally
        {
            viewModel.Complete(SafetyCountdownDecision.Revert);
            await dispatcher.InvokeAsync(() =>
            {
                window?.Close();
                _countdownWindow = null;
            });
        }
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

    private void Prepare(Window window)
    {
        theme.AttachWindow(window);
        DialogWindowPlacement.Prepare(window, System.Windows.Application.Current?.MainWindow);
    }
}
