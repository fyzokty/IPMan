using System.Text.Json;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.Infrastructure.Networking;

/// <summary>Atomic, durable JSON persistence for pre-mutation recovery snapshots.</summary>
public sealed class JsonRollbackSnapshotRepository : IRollbackSnapshotRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _backupDirectory;

    public JsonRollbackSnapshotRepository(RollbackSnapshotRepositoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.BackupDirectory))
        {
            throw new ArgumentException("Backup directory is required.", nameof(options));
        }

        _backupDirectory = Path.GetFullPath(options.BackupDirectory);
    }

    public async Task<RollbackCaptureResult> SaveAsync(
        NetworkRollbackSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        cancellationToken.ThrowIfCancellationRequested();

        string finalPath = Path.Combine(_backupDirectory, $"rollback-{snapshot.SnapshotId}.json");
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
                await JsonSerializer
                    .SerializeAsync(stream, snapshot, SerializerOptions, cancellationToken)
                    .ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryPath, finalPath, overwrite: false);
            return RollbackCaptureResult.Success(
                new RollbackSnapshotReference(snapshot.SnapshotId, finalPath));
        }
        catch (UnauthorizedAccessException)
        {
            return RollbackCaptureResult.Failed(RollbackCaptureFailure.AccessDenied);
        }
        catch (IOException)
        {
            return RollbackCaptureResult.Failed(RollbackCaptureFailure.IoFailure);
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
