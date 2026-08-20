using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

/// <summary>Persists and retrieves pre-mutation recovery snapshots.</summary>
public interface IRecoverySnapshotRepository
{
    /// <summary>Persists a recovery snapshot before a network mutation.</summary>
    Task<RecoveryCaptureResult> SaveAsync(
        RecoverySnapshot snapshot,
        CancellationToken cancellationToken);

    /// <summary>Loads the most recently captured supported snapshot for an adapter.</summary>
    Task<RecoverySnapshotLoadResult> LoadLatestAsync(
        NetworkAdapterId adapterId,
        CancellationToken cancellationToken);
}
