using IPMan.Application.Networking;
using IPMan.Domain.Networking;

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
        string temporaryPath = Path.Combine(
            _backupDirectory,
            $".{snapshot.SnapshotId}-{Guid.NewGuid():N}.tmp");

        try
        {
            Directory.CreateDirectory(_backupDirectory);

            await using (FileStream stream = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await _codec
                    .SerializeAsync(stream, snapshot, cancellationToken)
                    .ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryPath, finalPath, overwrite: false);
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
        finally
        {
            TryDeleteTemporaryFile(temporaryPath);
        }
    }

    private static void TryDeleteTemporaryFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // The final file is never partial; an orphaned temp can be ignored.
        }
        catch (UnauthorizedAccessException)
        {
            // Preserve the original typed persistence result.
        }
    }
}
