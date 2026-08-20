using System.Text.Json;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Common;

namespace IPMan.Infrastructure.Networking;

/// <summary>Atomic, durable JSON persistence for pre-mutation recovery snapshots.</summary>
public sealed class JsonRecoverySnapshotRepository : IRecoverySnapshotRepository
{
    /// <summary>The largest supported per-adapter snapshot retention limit.</summary>
    public const int MaximumRetentionLimitPerAdapter = 100;

    private readonly string _backupDirectory;
    private readonly int _retentionLimitPerAdapter;
    private readonly RecoverySnapshotJsonCodec _codec = new();

    /// <summary>Initializes a recovery repository with bounded per-adapter retention.</summary>
    public JsonRecoverySnapshotRepository(RecoverySnapshotRepositoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.BackupDirectory))
        {
            throw new ArgumentException("Backup directory is required.", nameof(options));
        }

        if (options.RetentionLimitPerAdapter is < 1 or > MaximumRetentionLimitPerAdapter)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.RetentionLimitPerAdapter,
                $"Retention must be between 1 and {MaximumRetentionLimitPerAdapter} snapshots per adapter.");
        }

        _backupDirectory = Path.GetFullPath(options.BackupDirectory);
        _retentionLimitPerAdapter = options.RetentionLimitPerAdapter;
    }

    /// <inheritdoc />
    public async Task<RecoveryCaptureResult> SaveAsync(
        RecoverySnapshot snapshot,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        cancellationToken.ThrowIfCancellationRequested();

        string finalPath = Path.Combine(_backupDirectory, $"recovery-{snapshot.SnapshotId}.json");
        try
        {
            await AtomicJsonFileWriter.WriteAsync(
                finalPath,
                overwrite: false,
                (stream, token) => _codec.SerializeAsync(stream, snapshot, token),
                cancellationToken).ConfigureAwait(false);

            await PruneAsync(snapshot.AdapterId, finalPath).ConfigureAwait(false);
            return RecoveryCaptureResult.Success(
                new RecoverySnapshotReference(snapshot.SnapshotId, finalPath));
        }
        catch (UnauthorizedAccessException)
        {
            return RecoveryCaptureResult.Failed(RecoveryCaptureFailure.AccessDenied);
        }
        catch (IOException)
        {
            return RecoveryCaptureResult.Failed(RecoveryCaptureFailure.IoFailure);
        }
    }

    /// <inheritdoc />
    public async Task<RecoverySnapshotLoadResult> LoadLatestAsync(
        NetworkAdapterId adapterId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string[] filePaths;
        try
        {
            filePaths = Directory.GetFiles(
                _backupDirectory,
                "recovery-*.json",
                SearchOption.TopDirectoryOnly);
        }
        catch (DirectoryNotFoundException)
        {
            return RecoverySnapshotLoadResult.Failed(RecoverySnapshotLoadStatus.NotFound);
        }
        catch (UnauthorizedAccessException)
        {
            return RecoverySnapshotLoadResult.Failed(RecoverySnapshotLoadStatus.AccessDenied);
        }
        catch (IOException)
        {
            return RecoverySnapshotLoadResult.Failed(RecoverySnapshotLoadStatus.IoFailure);
        }

        RecoverySnapshot? latest = null;
        foreach (string filePath in filePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                RecoverySnapshot snapshot = await ReadAsync(filePath, cancellationToken)
                    .ConfigureAwait(false);
                if (snapshot.SchemaVersion != RecoverySnapshotLoadResult.SupportedSchemaVersion ||
                    snapshot.AdapterId != adapterId)
                {
                    continue;
                }

                if (latest is null || snapshot.CapturedAtUtc > latest.CapturedAtUtc)
                {
                    latest = snapshot;
                }
            }
            catch (JsonException)
            {
                // A malformed snapshot is preserved and does not hide healthy snapshots.
            }
            catch (NotSupportedException)
            {
                // Unsupported JSON content is preserved for possible future recovery.
            }
            catch (UnauthorizedAccessException)
            {
                return RecoverySnapshotLoadResult.Failed(RecoverySnapshotLoadStatus.AccessDenied);
            }
            catch (IOException)
            {
                return RecoverySnapshotLoadResult.Failed(RecoverySnapshotLoadStatus.IoFailure);
            }
        }

        return latest is null
            ? RecoverySnapshotLoadResult.Failed(RecoverySnapshotLoadStatus.NotFound)
            : RecoverySnapshotLoadResult.Success(latest);
    }

    private async Task PruneAsync(NetworkAdapterId adapterId, string protectedPath)
    {
        string[] filePaths;
        try
        {
            filePaths = Directory.GetFiles(
                _backupDirectory,
                "recovery-*.json",
                SearchOption.TopDirectoryOnly);
        }
        catch (UnauthorizedAccessException)
        {
            // The snapshot is durable; a later save can retry retention cleanup.
            return;
        }
        catch (IOException)
        {
            // Preserve the successful snapshot write result.
            return;
        }

        List<StoredSnapshot> matching = new();
        foreach (string filePath in filePaths)
        {
            try
            {
                RecoverySnapshot snapshot = await ReadAsync(filePath, CancellationToken.None)
                    .ConfigureAwait(false);
                if (snapshot.SchemaVersion == RecoverySnapshotLoadResult.SupportedSchemaVersion &&
                    snapshot.AdapterId == adapterId)
                {
                    matching.Add(new StoredSnapshot(filePath, snapshot.CapturedAtUtc));
                }
            }
            catch (JsonException)
            {
                // The adapter identity is unknown, so preserve the file.
            }
            catch (NotSupportedException)
            {
                // Unknown content must not be pruned as if it belonged to this adapter.
            }
            catch (UnauthorizedAccessException)
            {
                // Preserve unreadable recovery data and continue best-effort cleanup.
            }
            catch (IOException)
            {
                // Preserve unreadable recovery data and continue best-effort cleanup.
            }
        }

        HashSet<string> retainedPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            protectedPath
        };
        foreach (StoredSnapshot stored in matching.OrderByDescending(item => item.CapturedAtUtc))
        {
            if (retainedPaths.Count >= _retentionLimitPerAdapter)
            {
                break;
            }

            retainedPaths.Add(stored.FilePath);
        }

        foreach (StoredSnapshot stored in matching.Where(item => !retainedPaths.Contains(item.FilePath)))
        {
            TryDeleteSnapshot(stored.FilePath);
        }
    }

    private async Task<RecoverySnapshot> ReadAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using FileStream stream = new(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return await _codec.DeserializeAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    private static void TryDeleteSnapshot(string filePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch (IOException)
        {
            // The saved snapshot is durable; incomplete retention can be retried later.
        }
        catch (UnauthorizedAccessException)
        {
            // Preserve the successful snapshot write result.
        }
    }

    private sealed record StoredSnapshot(string FilePath, DateTimeOffset CapturedAtUtc);
}
