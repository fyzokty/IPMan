using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.Application.Common;
using IPMan.Application.Networking;
using IPMan.Application.Settings;
using IPMan.Domain.Settings;

namespace IPMan.App.Presentation;

/// <summary>Single entry point for conditional confirmation, mandatory countdown and rollback.</summary>
public sealed class SafeSwitchCoordinator : ISafetyCountdownCoordinator
{
    private readonly IInternetLossConfirmationService _confirmation;
    private readonly ISafetyCountdownDialogService _dialogs;
    private readonly IAppSettingsRepository _settings;
    private readonly IRecoveryRestoreService _restore;
    private readonly IMonotonicClock _clock;
    private readonly ITimerService _timer;
    private readonly IUiDispatcher _dispatcher;
    private ActiveCountdown? _active;
    private int _running;
    private int _exitRequested;

    public SafeSwitchCoordinator(IInternetLossConfirmationService confirmation,
        ISafetyCountdownDialogService dialogs, IAppSettingsRepository settings,
        IRecoveryRestoreService restore, IMonotonicClock clock, ITimerService timer, IUiDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(confirmation);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(restore);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(timer);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _confirmation = confirmation;
        _dialogs = dialogs;
        _settings = settings;
        _restore = restore;
        _clock = clock;
        _timer = timer;
        _dispatcher = dispatcher;
    }

    public bool IsCountdownActive => _active is not null;

    public event EventHandler<AppSettings>? SettingsChanged;

    /// <inheritdoc cref="ISafetyCountdownCoordinator.RunAsync"/>
    public async Task<SafeSwitchOutcome> RunAsync(SafeSwitchRequest request,
        Func<CancellationToken, Task<SafeSwitchApplyResult>> applyAsync, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(applyAsync);
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            return new(SafeSwitchStatus.Busy, Strings.SafeSwitchBusy);
        }

        bool preferenceSaveFailed = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (InternetLossRiskRule.IsRisky(request.Adapter.InternetConnectivity, request.Target))
            {
                AppSettings settings = await _settings.LoadAsync(cancellationToken);
                if (!settings.SkipInternetLossConfirmation)
                {
                    InternetLossConfirmationResult confirmation = await _confirmation.ConfirmAsync(new(request), cancellationToken);
                    if (!confirmation.Accepted)
                    {
                        return new(SafeSwitchStatus.Cancelled);
                    }
                    if (confirmation.DoNotAskAgain)
                    {
                        // Reload after the modal to preserve preferences changed while it was open.
                        settings = await _settings.LoadAsync(cancellationToken);
                        settings = settings with { SkipInternetLossConfirmation = true };
                        SettingsSaveResult saved = await _settings.SaveAsync(
                            settings, cancellationToken);
                        preferenceSaveFailed = !saved.IsSuccess;
                        if (saved.IsSuccess)
                        {
                            AppSettings persisted = settings;
                            _dispatcher.Post(() => SettingsChanged?.Invoke(this, persisted));
                        }
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            SafeSwitchApplyResult applied;
            try
            {
                applied = await applyAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return new(SafeSwitchStatus.Cancelled, PreferenceSaveFailed: preferenceSaveFailed);
            }
            catch (Exception exception)
            {
                return new(SafeSwitchStatus.ApplyFailed, Strings.SafeSwitchApplyFailed,
                    Failure: exception, PreferenceSaveFailed: preferenceSaveFailed);
            }
            if (applied == SafeSwitchApplyResult.NoChange)
            {
                return new(SafeSwitchStatus.NoChange, PreferenceSaveFailed: preferenceSaveFailed);
            }
            if (applied is not (SafeSwitchApplyResult.Applied or SafeSwitchApplyResult.AppliedUnverified))
            {
                return new(SafeSwitchStatus.ApplyFailed, Strings.SafeSwitchApplyFailed,
                    PreferenceSaveFailed: preferenceSaveFailed);
            }

            // An unverified mutation may still have changed the network, so it is protected too.
            bool unverified = applied == SafeSwitchApplyResult.AppliedUnverified;
            using SafetyCountdownViewModel viewModel = new(request, _clock, _timer, unverified);
            ActiveCountdown active = new(viewModel);
            _active = active;
            // Once apply succeeded, caller cancellation must restore rather than abandon protection.
            using CancellationTokenRegistration registration = cancellationToken.Register(() =>
                _dispatcher.Post(() => viewModel.Complete(SafetyCountdownDecision.Revert)));
            SafeSwitchOutcome outcome;
            try
            {
                try
                {
                    viewModel.Start();
                    await _dialogs.ShowAsync(viewModel);
                }
                catch (Exception)
                {
                    viewModel.Complete(SafetyCountdownDecision.Revert);
                }

                SafetyCountdownDecision decision = await viewModel.Decision;
                outcome = decision == SafetyCountdownDecision.Keep
                    ? new(SafeSwitchStatus.Kept)
                    : await RestoreAsync(request);
                outcome = outcome with { PreferenceSaveFailed = preferenceSaveFailed, Unverified = unverified };
                active.Outcome.TrySetResult(outcome);
            }
            finally
            {
                _active = null;
            }
            return outcome;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new(SafeSwitchStatus.Cancelled, PreferenceSaveFailed: preferenceSaveFailed);
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }

    public async Task<bool> RequestExitAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _exitRequested, 1, 0) != 0)
        {
            return false;
        }
        try
        {
            ActiveCountdown? active = _active;
            if (active is null)
            {
                if (Volatile.Read(ref _running) == 0)
                {
                    return true;
                }
                // Applying cannot be interrupted safely; say so instead of ignoring the request.
                await _dialogs.ShowExitDeferredAsync(cancellationToken);
                return false;
            }
            if (!active.ViewModel.Decision.IsCompleted)
            {
                SafetyCountdownViewModel countdown = active.ViewModel;
                SafeSwitchExitDecision choice;
                // The deadline must not decide for the user while the exit question is open.
                countdown.Pause();
                try
                {
                    choice = await _dialogs.ConfirmExitAsync(cancellationToken);
                }
                finally
                {
                    countdown.Resume();
                }
                if (choice == SafeSwitchExitDecision.Cancel)
                {
                    return false;
                }
                SafetyCountdownDecision requested = choice == SafeSwitchExitDecision.Keep
                    ? SafetyCountdownDecision.Keep : SafetyCountdownDecision.Revert;
                if (!countdown.Complete(requested))
                {
                    // Something else (e.g. a lost adapter) decided while the question was open.
                    // Exit only if that matches the user's choice; otherwise the window stays so
                    // the real result banner is visible.
                    SafetyCountdownDecision actual = await countdown.Decision;
                    await active.Outcome.Task;
                    return actual == requested ||
                        (requested == SafetyCountdownDecision.Revert && actual == SafetyCountdownDecision.TimedOut);
                }
            }
            // Restore failures remain available through RunAsync for the result banner before exiting.
            await active.Outcome.Task;
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        finally
        {
            Interlocked.Exchange(ref _exitRequested, 0);
        }
    }

    private async Task<SafeSwitchOutcome> RestoreAsync(SafeSwitchRequest request)
    {
        try
        {
            RecoveryRestoreResult restored = await _restore.RestoreLatestAsync(
                new(request.Adapter.Id, ConfirmPotentialConflict: true, ContinueAfterIndeterminateProbe: true),
                CancellationToken.None);
            return restored.Status is RecoveryRestoreStatus.VerifiedSuccess or RecoveryRestoreStatus.NoChange
                ? new(SafeSwitchStatus.Reverted, RestoreResult: restored)
                : new(SafeSwitchStatus.RevertFailed, Strings.SafeSwitchRevertFailed, restored);
        }
        catch (Exception exception)
        {
            return new(SafeSwitchStatus.RevertFailed, Strings.SafeSwitchRevertFailed, Failure: exception);
        }
    }

    private sealed record ActiveCountdown(SafetyCountdownViewModel ViewModel)
    {
        public TaskCompletionSource<SafeSwitchOutcome> Outcome { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
