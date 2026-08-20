namespace IPMan.Application.Networking;

/// <summary>Restores the most recent supported recovery snapshot for an adapter.</summary>
public interface IRecoveryRestoreService
{
    /// <summary>Loads, identity-checks and delegates restoration of the latest snapshot.</summary>
    Task<RecoveryRestoreResult> RestoreLatestAsync(
        RecoveryRestoreRequest request,
        CancellationToken cancellationToken);
}
