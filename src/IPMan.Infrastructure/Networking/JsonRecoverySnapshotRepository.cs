using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Common;

namespace IPMan.Infrastructure.Networking;

/// <summary>Atomic, durable JSON persistence for pre-mutation recovery snapshots.</summary>
public sealed class JsonRecoverySnapshotRepository : IRecoverySnapshotRepository
{
    private readonly string _backupDirectory;
    private readonly RecoverySnapshotJsonCodec _codec = new();

    public JsonRecoverySnapshotRepository(RecoverySnapshotRepositoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.BackupDirectory))
        {
            throw new ArgumentException("Backup directory is required.", nameof(options));
        }

        _backupDirectory = Path.GetFullPath(options.BackupDirectory);
    }

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
}
