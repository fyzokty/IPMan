namespace IPMan.Infrastructure.Networking;

/// <summary>Configures recovery snapshot storage and per-adapter retention.</summary>
public sealed class RecoverySnapshotRepositoryOptions
{
    /// <summary>Gets the directory containing recovery snapshot JSON files.</summary>
    public required string BackupDirectory { get; init; }

    /// <summary>Gets the maximum number of healthy snapshots retained for each adapter.</summary>
    public int RetentionLimitPerAdapter { get; init; } = 10;
}
