using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

/// <summary>Result of locating an adapter's most recent supported recovery snapshot.</summary>
/// <param name="Snapshot">The loaded snapshot when the operation succeeded.</param>
/// <param name="Status">The load outcome.</param>
public sealed record RecoverySnapshotLoadResult(
    RecoverySnapshot? Snapshot,
    RecoverySnapshotLoadStatus Status)
{
    /// <summary>The only recovery snapshot schema understood by this application version.</summary>
    public const int SupportedSchemaVersion = 2;

    /// <summary>Gets whether a supported snapshot was loaded.</summary>
    public bool IsSuccess => Snapshot is not null && Status == RecoverySnapshotLoadStatus.Success;

    /// <summary>Creates a successful load result.</summary>
    public static RecoverySnapshotLoadResult Success(RecoverySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new(snapshot, RecoverySnapshotLoadStatus.Success);
    }

    /// <summary>Creates a failed load result.</summary>
    public static RecoverySnapshotLoadResult Failed(RecoverySnapshotLoadStatus status) =>
        new(null, status);
}
