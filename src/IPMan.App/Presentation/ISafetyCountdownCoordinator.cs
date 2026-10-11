using IPMan.Domain.Networking;
using IPMan.Application.Networking;
using IPMan.App.ViewModels;
using IPMan.Domain.Settings;

namespace IPMan.App.Presentation;

/// <summary>All UI apply delegates must use the existing locked, snapshot-capturing apply services.</summary>
public interface ISafetyCountdownCoordinator
{
    bool IsCountdownActive { get; }

    /// <summary>Lets the shell refresh its cached preferences after the opt-out is persisted.</summary>
    event EventHandler<AppSettings>? SettingsChanged;

    /// <summary>
    /// The apply delegate must return Applied only after a mutation with a pre-mutation recovery
    /// snapshot has been applied through the locked apply service. NoChange means the setting was
    /// already active: no countdown starts and no older snapshot is restored. Failed also starts
    /// no countdown.
    /// </summary>
    Task<SafeSwitchOutcome> RunAsync(SafeSwitchRequest request,
        Func<CancellationToken, Task<SafeSwitchApplyResult>> applyAsync, CancellationToken cancellationToken);

    /// <summary>Returns only after keep/restore finishes. False cancels exit or reports an active apply.</summary>
    Task<bool> RequestExitAsync(CancellationToken cancellationToken);
}

/// <summary>A null target represents the automatic (DHCP) setting.</summary>
public sealed record SafeSwitchRequest(NetworkAdapterSnapshot Adapter, string SettingName,
    string PreviousSettingName, StaticIpv4Configuration? Target);

/// <summary>Applied requires a mutation with a pre-mutation recovery snapshot; NoChange means no mutation.</summary>
public enum SafeSwitchApplyResult { Applied, NoChange, Failed }

public enum SafeSwitchStatus { Cancelled, Busy, ApplyFailed, Kept, Reverted, RevertFailed, NoChange }

/// <summary>Preserves restore diagnostics and localized guidance for an inline result banner.</summary>
public sealed record SafeSwitchOutcome(SafeSwitchStatus Status, string? Message = null,
    RecoveryRestoreResult? RestoreResult = null, Exception? Failure = null,
    bool PreferenceSaveFailed = false);

public sealed record InternetLossConfirmationResult(bool Accepted, bool DoNotAskAgain = false);

public interface IInternetLossConfirmationService
{
    Task<InternetLossConfirmationResult> ConfirmAsync(InternetLossConfirmationViewModel viewModel,
        CancellationToken cancellationToken);
}

public enum SafetyCountdownDecision { Keep, Revert, TimedOut }
public enum SafeSwitchExitDecision { Cancel, Keep, Revert }

public interface ISafetyCountdownDialogService
{
    /// <summary>Keeps the countdown visible until the view-model decision completes.</summary>
    Task ShowAsync(SafetyCountdownViewModel viewModel);

    Task<SafeSwitchExitDecision> ConfirmExitAsync(CancellationToken cancellationToken);
}
